namespace Zmg.Domain.Tests;

/// <summary>
/// One fixed "today" for the pure-domain tests (M25 task 8). These tests take <c>today</c> as an
/// explicit argument, so a fixed anchor is deterministic and safe — but four files each declared their
/// own slightly different date for no reason. Unified here.
/// </summary>
internal static class TestDates
{
    public static readonly DateOnly Today = new(2026, 7, 15);
}
