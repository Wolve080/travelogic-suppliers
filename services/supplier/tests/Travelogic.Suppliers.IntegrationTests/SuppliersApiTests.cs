using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Application.ReferenceData;
using Travelogic.Suppliers.Application.Suppliers;
using Travelogic.Suppliers.Domain.Suppliers;
using Travelogic.Suppliers.Infrastructure.Persistence;

namespace Travelogic.Suppliers.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class SuppliersApiTests(SupplierApiFactory factory)
{
    private const string Suppliers = "/api/v1/suppliers";
    private static readonly JsonSerializerOptions Json = SupplierApiFactory.Json;

    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..12]}";

    private static ServiceRequest HalfDayTour(string name = "Half Day Tour") =>
        new(name, ServiceCategory.Activity, "Morning game drive", 1450.50m, "ZAR", PricingUnit.PerPerson, 240, 9);

    private static CreateSupplierRequest NewSupplier(string name, params ServiceRequest[] services) =>
        new(
            name,
            SupplierType.Safari,
            "Guided safaris",
            new ContactDto("bookings@example.com", "+27 13 555 0199", "https://example.com"),
            new AddressDto(null, null, "Hazyview", "Mpumalanga", "South Africa", "1242"),
            services);

    private async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        var response = await _client.PostAsJsonAsync(Suppliers, request, Json);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SupplierResponse>(Json))!;
    }

    [Fact]
    public async Task Create_supplier_with_services_returns_201_with_location_and_body()
    {
        var name = UniqueName("Kruger Horizons");

        var response = await _client.PostAsJsonAsync(Suppliers, NewSupplier(name, HalfDayTour(), HalfDayTour("Sunset Walk")), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<SupplierResponse>(Json))!;
        response.Headers.Location!.AbsolutePath.ShouldBe($"{Suppliers}/{body.Id}");
        body.Name.ShouldBe(name);
        body.Type.ShouldBe(SupplierType.Safari);
        body.Address.City.ShouldBe("Hazyview");
        body.Services.Select(s => s.Name).ShouldBe(["Half Day Tour", "Sunset Walk"]);
        body.Services[0].Price.ShouldBe(1450.50m);
        body.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task List_returns_created_suppliers_with_service_counts_and_supports_search()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        await CreateAsync(NewSupplier($"Search {token} Hotel", HalfDayTour()));
        await CreateAsync(NewSupplier($"Search {token} Lodge"));
        await CreateAsync(NewSupplier(UniqueName("Unrelated")));

        var page = await _client.GetFromJsonAsync<PagedResult<SupplierSummaryResponse>>($"{Suppliers}?search={token}&sortBy=name", Json);

        page!.TotalCount.ShouldBe(2);
        page.Items.Select(s => s.Name).ShouldBe([$"Search {token} Hotel", $"Search {token} Lodge"]);
        page.Items[0].ServiceCount.ShouldBe(1);
        page.Items[0].ServiceCategories.ShouldBe([ServiceCategory.Activity]);
    }

    [Fact]
    public async Task List_pages_results()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            await CreateAsync(NewSupplier($"Paged {token} {i}"));
        }

        var page = await _client.GetFromJsonAsync<PagedResult<SupplierSummaryResponse>>($"{Suppliers}?search={token}&page=2&pageSize=2", Json);

        page!.Items.ShouldHaveSingleItem().Name.ShouldBe($"Paged {token} 2");
        page.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Invalid_request_returns_400_problem_details_with_field_errors()
    {
        var request = NewSupplier("", HalfDayTour() with { Currency = "RANDS", Price = -1 });

        var response = await _client.PostAsJsonAsync(Suppliers, request, Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("validation_failed");
        var errors = problem.RootElement.GetProperty("errors");
        errors.TryGetProperty("name", out _).ShouldBeTrue();
        errors.TryGetProperty("services[0].currency", out _).ShouldBeTrue();
        errors.TryGetProperty("services[0].price", out _).ShouldBeTrue();
        problem.RootElement.TryGetProperty("correlationId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Unknown_enum_value_returns_400()
    {
        var response = await _client.PostAsync(Suppliers, JsonContent.Create(new
        {
            name = UniqueName("Bad Enum"),
            type = "Spaceport",
            address = new { city = "Cape Town", country = "South Africa" },
        }));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_supplier_name_returns_409()
    {
        var name = UniqueName("Duplicate");
        await CreateAsync(NewSupplier(name));

        var response = await _client.PostAsJsonAsync(Suppliers, NewSupplier(name.ToUpperInvariant()), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("supplier_name_taken");
    }

    [Fact]
    public async Task Get_unknown_supplier_returns_404_problem()
    {
        var response = await _client.GetAsync($"{Suppliers}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("supplier_not_found");
    }

    [Fact]
    public async Task Update_uses_optimistic_concurrency()
    {
        var created = await CreateAsync(NewSupplier(UniqueName("Concurrency")));
        UpdateSupplierRequest Update(string name, string version) =>
            new(name, created.Type, created.Description, created.Contact, created.Address, true, version);

        var first = await _client.PutAsJsonAsync($"{Suppliers}/{created.Id}", Update(UniqueName("Renamed"), created.Version), Json);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await first.Content.ReadFromJsonAsync<SupplierResponse>(Json))!;
        updated.Version.ShouldNotBe(created.Version);
        updated.UpdatedAtUtc.ShouldNotBeNull();

        // A second writer still holding the original version must not overwrite the first change.
        var stale = await _client.PutAsJsonAsync($"{Suppliers}/{created.Id}", Update(UniqueName("Lost update"), created.Version), Json);
        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await stale.Content.ReadAsStringAsync()).ShouldContain("concurrency_conflict");
    }

    [Fact]
    public async Task Changing_a_service_changes_the_supplier_version()
    {
        var created = await CreateAsync(NewSupplier(UniqueName("Aggregate")));

        var added = await _client.PostAsJsonAsync($"{Suppliers}/{created.Id}/services", HalfDayTour(), Json);
        added.StatusCode.ShouldBe(HttpStatusCode.Created);

        var reloaded = await _client.GetFromJsonAsync<SupplierResponse>($"{Suppliers}/{created.Id}", Json);
        reloaded!.Version.ShouldNotBe(created.Version);
        reloaded.Services.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Services_can_be_added_updated_and_removed()
    {
        var supplier = await CreateAsync(NewSupplier(UniqueName("Services")));
        var servicesUrl = $"{Suppliers}/{supplier.Id}/services";

        var addResponse = await _client.PostAsJsonAsync(servicesUrl, HalfDayTour(), Json);
        addResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var service = (await addResponse.Content.ReadFromJsonAsync<ServiceResponse>(Json))!;
        addResponse.Headers.Location!.AbsolutePath.ShouldBe($"{servicesUrl}/{service.Id}");

        var duplicate = await _client.PostAsJsonAsync(servicesUrl, HalfDayTour("half day tour"), Json);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var update = await _client.PutAsJsonAsync($"{servicesUrl}/{service.Id}", HalfDayTour() with { Price = 1600m, PricingUnit = PricingUnit.PerGroup }, Json);
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await update.Content.ReadFromJsonAsync<ServiceResponse>(Json))!.Price.ShouldBe(1600m);

        (await _client.DeleteAsync($"{servicesUrl}/{service.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetAsync($"{servicesUrl}/{service.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_removes_the_supplier_and_its_services()
    {
        var supplier = await CreateAsync(NewSupplier(UniqueName("Delete me"), HalfDayTour()));

        (await _client.DeleteAsync($"{Suppliers}/{supplier.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await _client.GetAsync($"{Suppliers}/{supplier.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();
        (await db.SupplierServices.AnyAsync(s => s.SupplierId == supplier.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Changes_are_written_to_the_outbox_and_published()
    {
        var supplier = await CreateAsync(NewSupplier(UniqueName("Outbox"), HalfDayTour()));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();

        var deadline = DateTime.UtcNow.AddSeconds(15);
        List<string> published;
        do
        {
            await Task.Delay(200);
            published = await db.OutboxMessages.AsNoTracking()
                .Where(m => m.AggregateId == supplier.Id && m.ProcessedAtUtc != null)
                .OrderBy(m => m.OccurredAtUtc).ThenBy(m => m.Sequence)
                .Select(m => m.Type)
                .ToListAsync();
        }
        while (published.Count < 2 && DateTime.UtcNow < deadline);

        published.ShouldBe(["SupplierCreated", "SupplierServiceAdded"]);
    }

    [Fact]
    public async Task Correlation_id_is_echoed_back()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Suppliers);
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").ShouldHaveSingleItem().ShouldBe("test-correlation-123");
    }

    [Fact]
    public async Task Health_endpoints_report_healthy()
    {
        (await _client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var ready = await _client.GetAsync("/health/ready");
        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ready.Content.ReadAsStringAsync()).ShouldContain("database");
    }

    [Fact]
    public async Task Reference_data_lists_enum_options()
    {
        var data = await _client.GetFromJsonAsync<ReferenceDataResponse>("/api/v1/reference-data", Json);

        data!.SupplierTypes.ShouldContain(o => o.Value == "Safari");
        data.PricingUnits.ShouldContain(o => o.Label == "Per room per night");
    }
}
