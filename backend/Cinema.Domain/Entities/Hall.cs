using Cinema.Domain.Common;
using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class Hall
{
    public const int MaxRows = 40;
    public const int MaxCols = 40;

    private readonly List<Showtime> _showtimes = [];

    private Hall() { }

    public Hall(int cinemaBranchId, string name, int rows, int cols, SeatTypeCode[][] layout)
    {
        CinemaBranchId = cinemaBranchId;
        Rename(name);
        SetLayout(rows, cols, layout);
    }

    public int Id { get; private set; }
    public int CinemaBranchId { get; private set; }
    public CinemaBranch CinemaBranch { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int Rows { get; private set; }
    public int Cols { get; private set; }

    // JSON-матриця SeatTypeCode у форматі [[1,1,2,...], ...] — зберігається як рядок у БД.
    public string SeatLayoutJson { get; private set; } = default!;

    public IReadOnlyCollection<Showtime> Showtimes => _showtimes.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Hall name is required.");
        Name = name.Trim();
    }

    public void SetLayout(int rows, int cols, SeatTypeCode[][] layout)
    {
        if (rows is < 1 or > MaxRows)
            throw new DomainException($"Rows must be between 1 and {MaxRows}.");
        if (cols is < 1 or > MaxCols)
            throw new DomainException($"Cols must be between 1 and {MaxCols}.");
        if (layout.Length != rows)
            throw new DomainException("Layout rows count does not match Rows.");
        if (layout.Any(r => r.Length != cols))
            throw new DomainException("Every layout row must have exactly Cols entries.");

        Rows = rows;
        Cols = cols;
        SeatLayoutJson = System.Text.Json.JsonSerializer.Serialize(layout);
    }

    public void SetLayout(int rows, int cols, SeatTypeCode[,] layout)
    {
        if (rows is < 1 or > MaxRows)
            throw new DomainException($"Rows must be between 1 and {MaxRows}.");
        if (cols is < 1 or > MaxCols)
            throw new DomainException($"Cols must be between 1 and {MaxCols}.");
        if (layout.GetLength(0) != rows)
            throw new DomainException("Layout rows count does not match Rows.");
        if (layout.GetLength(1) != cols)
            throw new DomainException("Every layout row must have exactly Cols entries.");

        // Convert 2D array to jagged array for serialization
        var jagged = new SeatTypeCode[rows][];
        for (int i = 0; i < rows; i++)
        {
            jagged[i] = new SeatTypeCode[cols];
            for (int j = 0; j < cols; j++)
            {
                jagged[i][j] = layout[i, j];
            }
        }

        Rows = rows;
        Cols = cols;
        SeatLayoutJson = System.Text.Json.JsonSerializer.Serialize(jagged);
    }

    public SeatTypeCode[][] GetLayout()
    {
        return System.Text.Json.JsonSerializer.Deserialize<SeatTypeCode[][]>(SeatLayoutJson)
               ?? throw new DomainException("Hall layout JSON is corrupted.");
    }

    public SeatTypeCode SeatTypeAt(int row, int col)
    {
        if (row < 1 || row > Rows || col < 1 || col > Cols)
            throw new DomainException($"Seat ({row},{col}) is outside hall bounds {Rows}x{Cols}.");
        return GetLayout()[row - 1][col - 1];
    }
}
