namespace Cinema.Application.Reports;

public interface IReportExporter
{
    byte[] ExportSalesPdf(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to);
    byte[] ExportSalesXlsx(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to);
    byte[] ExportOccupancyPdf(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to);
    byte[] ExportOccupancyXlsx(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to);
}
