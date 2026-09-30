using Travelogic.Suppliers.Application.Common;
using Travelogic.Suppliers.Application.ReferenceData;
using Travelogic.Suppliers.Application.Suppliers;

namespace Travelogic.Suppliers.UnitTests.Application;

public class SupplierListQueryTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 500, 1, SupplierListQuery.MaxPageSize)]
    [InlineData(4, 25, 4, 25)]
    public void Paging_values_are_clamped(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var query = new SupplierListQuery { Page = page, PageSize = pageSize };

        query.Page.ShouldBe(expectedPage);
        query.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public void PagedResult_calculates_total_pages()
    {
        new PagedResult<int>([], 1, 20, 41).TotalPages.ShouldBe(3);
        new PagedResult<int>([], 1, 20, 0).TotalPages.ShouldBe(0);
    }

    [Fact]
    public void Reference_data_labels_are_human_readable()
    {
        ReferenceDataProvider.Get.PricingUnits.ShouldContain(new Option("PerRoomPerNight", "Per room per night"));
        ReferenceDataProvider.Get.SupplierTypes.ShouldContain(new Option("TourOperator", "Tour operator"));
    }
}
