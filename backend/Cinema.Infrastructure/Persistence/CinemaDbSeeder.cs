using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence;

public static class CinemaDbSeeder
{
    private static readonly string[] AllRoles = ["Admin", "Cashier", "Client"];

    public static async Task SeedAsync(
        CinemaDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, cancellationToken);
        await SeedCatalogAsync(db, cancellationToken);
        await SeedPromoCodesAsync(db, cancellationToken);
    }

    // --- ролі та дефолтні користувачі ---

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in AllRoles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, CancellationToken _)
    {
        await EnsureUser(userManager, "admin@cinema.local", "Admin_123!", "Admin", "Admin", "Admin");
        await EnsureUser(userManager, "cashier@cinema.local", "Cashier_123!", "Cashier", "Cashier", "Cashier");
    }

    private static async Task EnsureUser(
        UserManager<ApplicationUser> userManager,
        string email, string password,
        string firstName, string lastName,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user {email}: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
    }

    // --- каталог ---

    /// <summary>Публічний метод для використання в інтеграційних тестах.</summary>
    public static Task SeedCatalogForTestsAsync(CinemaDbContext db, CancellationToken ct = default)
        => SeedCatalogAsync(db, ct);

    private static async Task SeedCatalogAsync(CinemaDbContext db, CancellationToken cancellationToken)
    {
        if (await db.CinemaBranches.AnyAsync(cancellationToken))
            return;

        var kyiv = new CinemaBranch("Cinema Nova - Podil", "Kyiv", "Kontraktova Sq 10", "Europe/Kyiv");
        var lviv = new CinemaBranch("Cinema Nova - Opera", "Lviv", "Svobody Ave 27", "Europe/Kyiv");
        db.CinemaBranches.AddRange(kyiv, lviv);
        await db.SaveChangesAsync(cancellationToken);

        var halls = new[]
        {
            new Hall(kyiv.Id, "Hall 1 Standard", 8, 10, BuildLayout(8, 10, vipRows: [5, 6])),
            new Hall(kyiv.Id, "Hall 2 IMAX", 10, 12, BuildLayout(10, 12, vipRows: [6, 7, 8], loveRows: [10])),
            new Hall(lviv.Id, "Hall 1 Classic", 6, 8, BuildLayout(6, 8, vipRows: [4])),
            new Hall(lviv.Id, "Hall 2 VIP", 4, 6, BuildLayout(4, 6, vipRows: [1, 2, 3, 4]))
        };
        db.Halls.AddRange(halls);
        await db.SaveChangesAsync(cancellationToken);

        var genres = new Dictionary<string, Genre>
        {
            ["Action"] = new Genre("Action"),
            ["Drama"] = new Genre("Drama"),
            ["Sci-Fi"] = new Genre("Sci-Fi"),
            ["Comedy"] = new Genre("Comedy"),
            ["Animation"] = new Genre("Animation"),
            ["Thriller"] = new Genre("Thriller")
        };
        db.Genres.AddRange(genres.Values);
        await db.SaveChangesAsync(cancellationToken);

        var movies = new[]
        {
            CreateMovie("Starlight Odyssey", "A crew chases a signal across the galaxy.", 142, AgeRating.PG13, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), [genres["Sci-Fi"], genres["Action"]]),
            CreateMovie("The Last Violin", "A luthier's daughter uncovers a wartime secret.", 118, AgeRating.PG, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), [genres["Drama"]]),
            CreateMovie("Neon Pulse", "A courier hunts stolen AI cores through a rain-soaked metropolis.", 109, AgeRating.R, new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc), [genres["Action"], genres["Thriller"]]),
            CreateMovie("Little Brave Squirrel", "An unlikely hero saves the forest from a winter curse.", 88, AgeRating.G, new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), [genres["Animation"], genres["Comedy"]]),
            CreateMovie("Parallel Lines", "Two timelines intersect when a physicist makes a promise.", 131, AgeRating.PG13, new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), [genres["Sci-Fi"], genres["Drama"]]),
            CreateMovie("Quiet Things", "A mute diver discovers something ancient under the ice.", 96, AgeRating.PG13, new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc), [genres["Thriller"], genres["Drama"]])
        };
        db.Movies.AddRange(movies);
        await db.SaveChangesAsync(cancellationToken);

        var baseStart = DateTime.UtcNow.AddDays(1).Date.AddHours(14);
        var showtimes = new List<Showtime>();
        for (var day = 0; day < 5; day++)
        {
            foreach (var hall in halls)
            {
                var movie = movies[(day + hall.Id) % movies.Length];
                var start = baseStart.AddDays(day).AddHours(hall.Id % 4);
                var format = (hall.Rows >= 10) ? MovieFormat.Imax : MovieFormat.TwoD;
                var basePrice = format == MovieFormat.Imax ? 220m : 150m;
                showtimes.Add(new Showtime(movie.Id, hall.Id, start, format, basePrice));
                if (showtimes.Count == 20) break;
            }
            if (showtimes.Count == 20) break;
        }
        db.Showtimes.AddRange(showtimes);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPromoCodesAsync(CinemaDbContext db, CancellationToken ct)
    {
        if (await db.PromoCodes.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var far = now.AddYears(2);

        var codes = new[]
        {
            // 10% знижка для всіх, необмежена кількість використань
            new PromoCode("WELCOME10",  DiscountType.Percent, 10, now, far, usageLimit: 0, perUserLimit: 1),
            // 50 грн фіксована знижка, до 100 використань
            new PromoCode("SAVE50",     DiscountType.Fixed,   50, now, far, usageLimit: 100, perUserLimit: 2),
            // 20% для зареєстрованих, до 200 використань
            new PromoCode("MEMBER20",   DiscountType.Percent, 20, now, far, usageLimit: 200, perUserLimit: 1),
            // Прострочений код — для перевірки валідації
            new PromoCode("EXPIRED",    DiscountType.Percent, 15,
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                usageLimit: 0, perUserLimit: 1),
        };
        db.PromoCodes.AddRange(codes);
        await db.SaveChangesAsync(ct);
    }

    private static Movie CreateMovie(string title, string description, int duration, AgeRating rating, DateTime releaseUtc, Genre[] genres)
    {
        var movie = new Movie(title, description, duration, rating, releaseUtc,
            posterUrl: $"https://placehold.co/400x600?text={Uri.EscapeDataString(title)}",
            trailerUrl: null);
        foreach (var g in genres)
            movie.AddGenre(g);
        return movie;
    }

    private static SeatTypeCode[][] BuildLayout(int rows, int cols, int[]? vipRows = null, int[]? loveRows = null)
    {
        var layout = new SeatTypeCode[rows][];
        for (var r = 0; r < rows; r++)
        {
            layout[r] = new SeatTypeCode[cols];
            for (var c = 0; c < cols; c++)
                layout[r][c] = SeatTypeCode.Standard;
        }

        if (vipRows is not null)
            foreach (var r in vipRows)
                if (r >= 1 && r <= rows)
                    for (var c = 0; c < cols; c++)
                        layout[r - 1][c] = SeatTypeCode.Vip;

        if (loveRows is not null)
            foreach (var r in loveRows)
                if (r >= 1 && r <= rows)
                    for (var c = 0; c < cols; c++)
                        layout[r - 1][c] = SeatTypeCode.Love;

        return layout;
    }
}
