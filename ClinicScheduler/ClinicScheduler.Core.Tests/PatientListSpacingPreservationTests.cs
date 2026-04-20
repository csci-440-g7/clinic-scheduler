using System.Text.RegularExpressions;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Preservation property tests for the patient list spacing fix.
/// These tests assert that non-spacing visual CSS properties remain unchanged
/// after the spacing fix is applied.
///
/// On UNFIXED code, all tests should PASS — confirming the baseline values to preserve.
/// After the fix, all tests should STILL PASS — confirming no regressions.
///
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
/// </summary>
public class PatientListSpacingPreservationTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".kiro")) &&
                Directory.Exists(Path.Combine(dir, "ClinicScheduler")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return fallback;
    }

    private static string ReadPatientsRazorContent()
    {
        var fullPath = Path.Combine(RepoRoot, "ClinicScheduler", "ClinicScheduler.Shared", "Pages", "Patients.razor");
        File.Exists(fullPath).Should().BeTrue($"Expected Patients.razor at {fullPath}");
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Extracts the scoped style block content from the Razor file.
    /// </summary>
    private static string ExtractStyleBlock(string razorContent)
    {
        var match = Regex.Match(razorContent, @"<style>(.*?)</style>", RegexOptions.Singleline);
        match.Success.Should().BeTrue("Patients.razor must contain a <style> block");
        return match.Groups[1].Value;
    }

    /// <summary>
    /// Parses a specific CSS property value from a CSS rule block for the given selector.
    /// Returns null if the property is not found.
    /// </summary>
    private static string? GetCssPropertyValue(string styleBlock, string selector, string property)
    {
        var escapedSelector = Regex.Escape(selector);
        var ruleMatch = Regex.Match(styleBlock, escapedSelector + @"\s*\{([^}]*)\}", RegexOptions.Singleline);
        if (!ruleMatch.Success)
            return null;

        var declarations = ruleMatch.Groups[1].Value;
        var propMatch = Regex.Match(declarations, @"(?:^|;)\s*" + Regex.Escape(property) + @"\s*:\s*([^;]+)", RegexOptions.Singleline);
        if (!propMatch.Success)
            return null;

        return propMatch.Groups[1].Value.Trim();
    }

    // ---------------------------------------------------------------
    // Requirement 3.1: .list-title font-weight: 700; font-size: 15px
    // ---------------------------------------------------------------

    /// <summary>
    /// .list-title must retain font-weight: 700.
    ///
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void ListTitle_FontWeight_Should_Be_700()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var fontWeight = GetCssPropertyValue(styleBlock, ".list-title", "font-weight");
        fontWeight.Should().NotBeNull(".list-title must have an explicit font-weight");
        fontWeight.Should().Be("700", ".list-title font-weight must remain 700");
    }

    /// <summary>
    /// .list-title must retain font-size: 15px.
    ///
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Fact]
    public void ListTitle_FontSize_Should_Be_15px()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var fontSize = GetCssPropertyValue(styleBlock, ".list-title", "font-size");
        fontSize.Should().NotBeNull(".list-title must have an explicit font-size");
        fontSize.Should().Be("15px", ".list-title font-size must remain 15px");
    }

    // ---------------------------------------------------------------
    // Requirement 3.2: .list-sub color: #6b7280; font-size: 13px
    // ---------------------------------------------------------------

    /// <summary>
    /// .list-sub must retain color: #6b7280.
    ///
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Fact]
    public void ListSub_Color_Should_Be_Gray()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var color = GetCssPropertyValue(styleBlock, ".list-sub", "color");
        color.Should().NotBeNull(".list-sub must have an explicit color");
        color.Should().Be("#6b7280", ".list-sub color must remain #6b7280");
    }

    /// <summary>
    /// .list-sub must retain font-size: 13px.
    ///
    /// **Validates: Requirements 3.2**
    /// </summary>
    [Fact]
    public void ListSub_FontSize_Should_Be_13px()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var fontSize = GetCssPropertyValue(styleBlock, ".list-sub", "font-size");
        fontSize.Should().NotBeNull(".list-sub must have an explicit font-size");
        fontSize.Should().Be("13px", ".list-sub font-size must remain 13px");
    }

    // ---------------------------------------------------------------
    // Requirement 3.3: .list-hint font-size: 12px; font-weight: 600; color: #2563eb
    // ---------------------------------------------------------------

    /// <summary>
    /// .list-hint must retain font-size: 12px.
    ///
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public void ListHint_FontSize_Should_Be_12px()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var fontSize = GetCssPropertyValue(styleBlock, ".list-hint", "font-size");
        fontSize.Should().NotBeNull(".list-hint must have an explicit font-size");
        fontSize.Should().Be("12px", ".list-hint font-size must remain 12px");
    }

    /// <summary>
    /// .list-hint must retain font-weight: 600.
    ///
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public void ListHint_FontWeight_Should_Be_600()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var fontWeight = GetCssPropertyValue(styleBlock, ".list-hint", "font-weight");
        fontWeight.Should().NotBeNull(".list-hint must have an explicit font-weight");
        fontWeight.Should().Be("600", ".list-hint font-weight must remain 600");
    }

    /// <summary>
    /// .list-hint must retain color: #2563eb.
    ///
    /// **Validates: Requirements 3.3**
    /// </summary>
    [Fact]
    public void ListHint_Color_Should_Be_Blue()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var color = GetCssPropertyValue(styleBlock, ".list-hint", "color");
        color.Should().NotBeNull(".list-hint must have an explicit color");
        color.Should().Be("#2563eb", ".list-hint color must remain #2563eb");
    }

    // ---------------------------------------------------------------
    // Requirement 3.4: .list-item-btn:hover background: #eff6ff
    // ---------------------------------------------------------------

    /// <summary>
    /// .list-item-btn:hover must retain background: #eff6ff.
    ///
    /// **Validates: Requirements 3.4**
    /// </summary>
    [Fact]
    public void ListItemBtnHover_Background_Should_Be_LightBlue()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var background = GetCssPropertyValue(styleBlock, ".list-item-btn:hover", "background");
        background.Should().NotBeNull(".list-item-btn:hover must have an explicit background");
        background.Should().Be("#eff6ff", ".list-item-btn:hover background must remain #eff6ff");
    }

    // ---------------------------------------------------------------
    // Requirement 3.5: .list-item border, border-radius, background
    //                   .list-item-btn padding
    // ---------------------------------------------------------------

    /// <summary>
    /// .list-item must retain border: 1px solid #e5e7eb.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void ListItem_Border_Should_Be_Preserved()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var border = GetCssPropertyValue(styleBlock, ".list-item", "border");
        border.Should().NotBeNull(".list-item must have an explicit border");
        border.Should().Be("1px solid #e5e7eb", ".list-item border must remain 1px solid #e5e7eb");
    }

    /// <summary>
    /// .list-item must retain border-radius: 10px.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void ListItem_BorderRadius_Should_Be_10px()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var borderRadius = GetCssPropertyValue(styleBlock, ".list-item", "border-radius");
        borderRadius.Should().NotBeNull(".list-item must have an explicit border-radius");
        borderRadius.Should().Be("10px", ".list-item border-radius must remain 10px");
    }

    /// <summary>
    /// .list-item must retain background: #fafafa.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void ListItem_Background_Should_Be_Preserved()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var background = GetCssPropertyValue(styleBlock, ".list-item", "background");
        background.Should().NotBeNull(".list-item must have an explicit background");
        background.Should().Be("#fafafa", ".list-item background must remain #fafafa");
    }

    /// <summary>
    /// .list-item-btn must retain padding: 14px 16px.
    ///
    /// **Validates: Requirements 3.5**
    /// </summary>
    [Fact]
    public void ListItemBtn_Padding_Should_Be_Preserved()
    {
        var styleBlock = ExtractStyleBlock(ReadPatientsRazorContent());

        var padding = GetCssPropertyValue(styleBlock, ".list-item-btn", "padding");
        padding.Should().NotBeNull(".list-item-btn must have an explicit padding");
        padding.Should().Be("14px 16px", ".list-item-btn padding must remain 14px 16px");
    }
}
