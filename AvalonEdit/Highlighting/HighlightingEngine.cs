using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using AvalonEdit.Document;
using AvalonEdit.Utils;
using SpanStack = AvalonEdit.Utils.ImmutableStack<AvalonEdit.Highlighting.HighlightingSpan>;

namespace AvalonEdit.Highlighting;

public class HighlightingEngine
{
    private readonly HighlightingRuleSet mainRuleSet;
    private          SpanStack           spanStack = SpanStack.Empty;

    public HighlightingEngine(HighlightingRuleSet mainRuleSet)
    {
        this.mainRuleSet = mainRuleSet ?? throw new ArgumentNullException(nameof(mainRuleSet));
    }

    public SpanStack CurrentSpanStack { get => spanStack; set => spanStack = value ?? SpanStack.Empty; }

    #region Highlighting Engine

    // local variables from HighlightLineInternal (are member because they are accessed by HighlighLine helper methods)
    private string lineText;
    private int    lineStartOffset;
    private int    position;

    private HighlightedLine highlightedLine;

    public HighlightedLine HighlightLine(IDocument document, IDocumentLine line)
    {
        lineStartOffset = line.Offset;
        lineText        = document.GetText(line);
        try
        {
            highlightedLine = new HighlightedLine(document, line);
            HighlightLineInternal();
            return highlightedLine;
        }
        finally
        {
            highlightedLine = null;
            lineText        = null;
            lineStartOffset = 0;
        }
    }

    public void ScanLine(IDocument document, IDocumentLine line)
    {
        //this.lineStartOffset = line.Offset; not necessary for scanning
        lineText = document.GetText(line);
        try
        {
            Debug.Assert(highlightedLine == null);
            HighlightLineInternal();
        }
        finally
        {
            lineText = null;
        }
    }

    private void HighlightLineInternal()
    {
        position = 0;
        ResetColorStack();
        var   currentRuleSet    = CurrentRuleSet;
        var   storedMatchArrays = new Stack<Match[]>();
        var   matches           = AllocateMatchArray(currentRuleSet.Spans.Count);
        Match endSpanMatch      = null;

        while (true)
        {
            for (var i = 0; i < matches.Length; i++)
                if (matches[i] == null || (matches[i].Success && matches[i].Index < position))
                    matches[i] = currentRuleSet.Spans[i].StartExpression.Match(lineText, position);
            if (!spanStack.IsEmpty) endSpanMatch = spanStack.Peek().EndExpression.Match(lineText, position);

            var firstMatch = Minimum(matches, endSpanMatch);
            if (firstMatch == null) break;

            HighlightNonSpans(firstMatch.Index);

            Debug.Assert(position == firstMatch.Index);

            if (firstMatch == endSpanMatch)
            {
                var poppedSpan = spanStack.Peek();
                if (!poppedSpan.SpanColorIncludesEnd) PopColor(); // pop SpanColor
                PushColor(poppedSpan.EndColor);
                position = firstMatch.Index + firstMatch.Length;
                PopColor();                                      // pop EndColor
                if (poppedSpan.SpanColorIncludesEnd) PopColor(); // pop SpanColor
                spanStack      = spanStack.Pop();
                currentRuleSet = CurrentRuleSet;
                //FreeMatchArray(matches);
                if (storedMatchArrays.Count > 0)
                {
                    matches = storedMatchArrays.Pop();
                    var index = currentRuleSet.Spans.IndexOf(poppedSpan);
                    Debug.Assert(index >= 0 && index < matches.Length);
                    if (matches[index].Index == position) throw new InvalidOperationException("A highlighting span matched 0 characters, which would cause an endless loop.\n" + "Change the highlighting definition so that either the start or the end regex matches at least one character.\n" + "Start regex: " + poppedSpan.StartExpression + "\n" + "End regex: " + poppedSpan.EndExpression);
                }
                else
                {
                    matches = AllocateMatchArray(currentRuleSet.Spans.Count);
                }
            }
            else
            {
                var index = Array.IndexOf(matches, firstMatch);
                Debug.Assert(index >= 0);
                var newSpan = currentRuleSet.Spans[index];
                spanStack      = spanStack.Push(newSpan);
                currentRuleSet = CurrentRuleSet;
                storedMatchArrays.Push(matches);
                matches = AllocateMatchArray(currentRuleSet.Spans.Count);
                if (newSpan.SpanColorIncludesStart) PushColor(newSpan.SpanColor);
                PushColor(newSpan.StartColor);
                position = firstMatch.Index + firstMatch.Length;
                PopColor();
                if (!newSpan.SpanColorIncludesStart) PushColor(newSpan.SpanColor);
            }

            endSpanMatch = null;
        }

        HighlightNonSpans(lineText.Length);

        PopAllColors();
    }

    private void HighlightNonSpans(int until)
    {
        Debug.Assert(position <= until);
        if (position == until) return;
        if (highlightedLine != null)
        {
            var rules   = CurrentRuleSet.Rules;
            var matches = AllocateMatchArray(rules.Count);
            while (true)
            {
                for (var i = 0; i < matches.Length; i++)
                    if (matches[i] == null || (matches[i].Success && matches[i].Index < position))
                        matches[i] = rules[i].Regex.Match(lineText, position, until - position);
                var firstMatch = Minimum(matches, null);
                if (firstMatch == null) break;

                position = firstMatch.Index;
                var ruleIndex = Array.IndexOf(matches, firstMatch);
                if (firstMatch.Length == 0) throw new InvalidOperationException("A highlighting rule matched 0 characters, which would cause an endless loop.\n" + "Change the highlighting definition so that the rule matches at least one character.\n" + "Regex: " + rules[ruleIndex].Regex);
                PushColor(rules[ruleIndex].Color);
                position = firstMatch.Index + firstMatch.Length;
                PopColor();
            }
            //FreeMatchArray(matches);
        }

        position = until;
    }

    private static readonly HighlightingRuleSet emptyRuleSet = new() { Name = "EmptyRuleSet" };

    private HighlightingRuleSet CurrentRuleSet
    {
        get
        {
            if (spanStack.IsEmpty) return mainRuleSet;
            return spanStack.Peek().RuleSet ?? emptyRuleSet;
        }
    }

    #endregion

    #region Color Stack Management

    private Stack<HighlightedSection> highlightedSectionStack;
    private HighlightedSection        lastPoppedSection;

    private void ResetColorStack()
    {
        Debug.Assert(position == 0);
        lastPoppedSection = null;
        if (highlightedLine == null)
        {
            highlightedSectionStack = null;
        }
        else
        {
            highlightedSectionStack = new Stack<HighlightedSection>();
            foreach (var span in spanStack.Reverse()) PushColor(span.SpanColor);
        }
    }

    private void PushColor(HighlightingColor color)
    {
        if (highlightedLine == null) return;
        if (color == null)
        {
            highlightedSectionStack.Push(null);
        }
        else if (lastPoppedSection != null && lastPoppedSection.Color.Equals(color) && lastPoppedSection.Offset + lastPoppedSection.Length == position + lineStartOffset)
        {
            highlightedSectionStack.Push(lastPoppedSection);
            lastPoppedSection = null;
        }
        else
        {
            var hs = new HighlightedSection { Offset = position + lineStartOffset, Color = color };
            highlightedLine.Sections.Add(hs);
            highlightedSectionStack.Push(hs);
            lastPoppedSection = null;
        }
    }

    private void PopColor()
    {
        if (highlightedLine == null) return;
        var s = highlightedSectionStack.Pop();
        if (s != null)
        {
            s.Length = position + lineStartOffset - s.Offset;
            if (s.Length == 0) highlightedLine.Sections.Remove(s);
            else lastPoppedSection = s;
        }
    }

    private void PopAllColors()
    {
        if (highlightedSectionStack == null) return;
        while (highlightedSectionStack.Count > 0) PopColor();
    }

    #endregion

    #region Match helpers

    private static Match Minimum(IEnumerable<Match> arr, Match endSpanMatch)
    {
        Match min = null;
        foreach (var v in arr)
            if (v.Success && (min == null || v.Index < min.Index))
                min = v;
        if (endSpanMatch is { Success: true } && (min == null || endSpanMatch.Index < min.Index)) return endSpanMatch;
        return min;
    }

    private static Match[] AllocateMatchArray(int count)
    {
        return count == 0 ? Empty<Match>.Array : new Match[count];
    }

    #endregion
}