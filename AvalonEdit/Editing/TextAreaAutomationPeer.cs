using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using AvalonEdit.Document;

namespace AvalonEdit.Editing;

internal class TextAreaAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, ITextProvider
{
    public TextAreaAutomationPeer(TextArea owner) : base(owner)
    {
        owner.Caret.PositionChanged += OnSelectionChanged;
        owner.SelectionChanged      += OnSelectionChanged;
    }

    private void OnSelectionChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged)");
        RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);
    }

    private TextArea TextArea => (TextArea)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Document;
    }

    internal IRawElementProviderSimple Provider => ProviderFromPeer(this);

    public bool IsReadOnly => TextArea.ReadOnlySectionProvider == ReadOnlySectionDocument.Instance;

    public void SetValue(string value)
    {
        TextArea.Document.Text = value;
    }

    public string Value => TextArea.Document.Text;

    public ITextRangeProvider DocumentRange
    {
        get
        {
            Debug.WriteLine("TextAreaAutomationPeer.get_DocumentRange()");
            return new TextRangeProvider(TextArea, TextArea.Document, 0, TextArea.Document.TextLength);
        }
    }

    public ITextRangeProvider[] GetSelection()
    {
        Debug.WriteLine("TextAreaAutomationPeer.GetSelection()");
        if (!TextArea.Selection.IsEmpty) return TextArea.Selection.Segments.Select(s => new TextRangeProvider(TextArea, TextArea.Document, s)).ToArray();
        var anchor = TextArea.Document.CreateAnchor(TextArea.Caret.Offset);
        anchor.SurviveDeletion = true;
        return new ITextRangeProvider[] { new TextRangeProvider(TextArea, TextArea.Document, new AnchorSegment(anchor, anchor)) };

    }

    public ITextRangeProvider[] GetVisibleRanges()
    {
        Debug.WriteLine("TextAreaAutomationPeer.GetVisibleRanges()");
        throw new NotImplementedException();
    }

    public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
    {
        Debug.WriteLine("TextAreaAutomationPeer.RangeFromChild()");
        throw new NotImplementedException();
    }

    public ITextRangeProvider RangeFromPoint(Point screenLocation)
    {
        Debug.WriteLine("TextAreaAutomationPeer.RangeFromPoint()");
        throw new NotImplementedException();
    }

    public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

    public override object GetPattern(PatternInterface patternInterface)
    {
        switch (patternInterface)
        {
            case PatternInterface.Text:
                return this;
            case PatternInterface.Value:
                return this;
            case PatternInterface.Scroll:
            {
                if (TextArea.GetService(typeof(TextEditor)) is TextEditor editor)
                {
                    var fromElement = FromElement(editor);
                    if (fromElement != null) return fromElement.GetPattern(patternInterface);
                }

                break;
            }
        }

        return base.GetPattern(patternInterface);
    }
}