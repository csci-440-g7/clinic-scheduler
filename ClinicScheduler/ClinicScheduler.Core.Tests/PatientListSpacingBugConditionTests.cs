using System.Text.RegularExpressions;
using FluentAssertions;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Bug condition exploration tests for the patient list spacing fix.
/// These tests assert the EXPECTED (fixed) spacing behavior against the CURRENT (unfixed) CSS
/// in the scoped style block of Patients.razor.
///
/// On unfixed code, all tests should FAIL — confirming the spacing bugs exist.
///
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4**
/// </summary>
public class PatientListSpacingBugConditionTests
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
        // Escape the dot in the selector for regex
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

    /// <summary>
    /// Parses a pixel value string (e.g. "4px") into its numeric value.
    /// </summary>
    private static int ParsePx(string value)
    {
        var match = Regex.Match(value, @"(\d+)px");
        match.Success.Should().BeTrue($"Expected a pixel value but got '{value}'");
        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>
    /// Bug Condition 1.1: .list-title margin-bottom must be >= 6px.
    /// Current value is 4px — test will FAIL on unfixed code.
    ///
    /// **Validates: Requirements 1.1**
    /// </summary>
    [Fact]
    public void ListTitle_MarginBottom_Should_Be_At_Least_6px()
    {
        var content = ReadPatientsRazorContent();
        var styleBlock = ExtractStyleBlock(content);

        var marginBottom = GetCssPropertyValue(styleBlock, ".list-title", "margin-bottom");
        marginBottom.Should().NotBeNull("'.list-title' must have an explicit margin-bottom");

        var px = ParsePx(marginBottom!);
        px.Should().BeGreaterThanOrEqualTo(6,
            ".list-title margin-bottom must be >= 6px for sufficient gap between name and first data field");
    }

    /// <summary>
    /// Bug Condition 1.2: .list-sub margin-top must be >= 4px.
    /// Current value is 2px — test will FAIL on unfixed code.
    ///
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Fact]
    public void ListSub_MarginTop_Should_Be_At_Least_4px()
    {
        var content = ReadPatientsRazorContent();
        var styleBlock = ExtractStyleBlock(content);

        var marginTop = GetCssPropertyValue(styleBlock, ".list-sub", "margin-top");
        marginTop.Should().NotBeNull("'.list-sub' must have an explicit margin-top");

        var px = ParsePx(marginTop!);
        px.Should().BeGreaterThanOrEqualTo(4,
            ".list-sub margin-top must be >= 4px for sufficient gap between consecutive data fields");
    }

    /// <summary>
    /// Bug Condition 1.3: .list-hint margin-top must be proportional to .list-sub margin-top
    /// (no more than 2×). Current: 10px vs 2×2px=4px — test will FAIL on unfixed code.
    ///
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void ListHint_MarginTop_Should_Be_Proportional_To_ListSub_MarginTop()
    {
        var content = ReadPatientsRazorContent();
        var styleBlock = ExtractStyleBlock(content);

        var subMarginTopStr = GetCssPropertyValue(styleBlock, ".list-sub", "margin-top");
        subMarginTopStr.Should().NotBeNull("'.list-sub' must have an explicit margin-top");
        var subMarginTop = ParsePx(subMarginTopStr!);

        var hintMarginTopStr = GetCssPropertyValue(styleBlock, ".list-hint", "margin-top");
        hintMarginTopStr.Should().NotBeNull("'.list-hint' must have an explicit margin-top");
        var hintMarginTop = ParsePx(hintMarginTopStr!);

        var maxAllowed = 2 * subMarginTop;
        hintMarginTop.Should().BeLessThanOrEqualTo(maxAllowed,
            $".list-hint margin-top ({hintMarginTop}px) must be no more than 2× .list-sub margin-top ({subMarginTop}px = max {maxAllowed}px)");
    }

    /// <summary>
    /// Bug Condition 1.4: All three classes must have explicit line-height set.
    /// None currently have it — test will FAIL on unfixed code.
    ///
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Fact]
    public void AllListClasses_Should_Have_Explicit_LineHeight()
    {
        var content = ReadPatientsRazorContent();
        var styleBlock = ExtractStyleBlock(content);

        var titleLineHeight = GetCssPropertyValue(styleBlock, ".list-title", "line-height");
        var subLineHeight = GetCssPropertyValue(styleBlock, ".list-sub", "line-height");
        var hintLineHeight = GetCssPropertyValue(styleBlock, ".list-hint", "line-height");

        titleLineHeight.Should().NotBeNull(".list-title must have an explicit line-height for readability");
        subLineHeight.Should().NotBeNull(".list-sub must have an explicit line-height for readability");
        hintLineHeight.Should().NotBeNull(".list-hint must have an explicit line-height for readability");
    }
}
