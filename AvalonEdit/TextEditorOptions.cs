using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AvalonEdit;

[Serializable]
public class TextEditorOptions : INotifyPropertyChanged
{
    #region ctor

    public TextEditorOptions()
    {
    }

    public TextEditorOptions(TextEditorOptions options)
    {
        var fields = typeof(TextEditorOptions).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        foreach (var fi in fields)
            if (!fi.IsNotSerialized)
                fi.SetValue(this, fi.GetValue(options));
    }

    #endregion

    #region PropertyChanged handling

    [field: NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    #endregion

    #region ShowSpaces / ShowTabs / ShowEndOfLine / ShowBoxForControlCharacters

    private bool showSpaces;

    [DefaultValue(false)]
    public virtual bool ShowSpaces
    {
        get => showSpaces;
        set
        {
            if (showSpaces != value)
            {
                showSpaces = value;
                OnPropertyChanged("ShowSpaces");
            }
        }
    }

    private bool showTabs;

    [DefaultValue(false)]
    public virtual bool ShowTabs
    {
        get => showTabs;
        set
        {
            if (showTabs == value) return;
            showTabs = value;
            OnPropertyChanged("ShowTabs");
        }
    }

    private bool showEndOfLine;

    [DefaultValue(false)]
    public virtual bool ShowEndOfLine
    {
        get => showEndOfLine;
        set
        {
            if (showEndOfLine == value) return;
            showEndOfLine = value;
            OnPropertyChanged("ShowEndOfLine");
        }
    }

    private bool showBoxForControlCharacters = true;

    [DefaultValue(true)]
    public virtual bool ShowBoxForControlCharacters
    {
        get => showBoxForControlCharacters;
        set
        {
            if (showBoxForControlCharacters == value) return;
            showBoxForControlCharacters = value;
            OnPropertyChanged("ShowBoxForControlCharacters");
        }
    }

    #endregion

    #region EnableHyperlinks

    //private bool enableHyperlinks = false;
    //
    // [DefaultValue(true)]
    // public virtual bool EnableHyperlinks
    // {
    //     get => enableHyperlinks;
    //     set
    //     {
    //         if (enableHyperlinks == value) return;
    //         enableHyperlinks = value;
    //         OnPropertyChanged("EnableHyperlinks");
    //     }
    // }
    //
    // private bool enableEmailHyperlinks = true;
    //
    // [DefaultValue(true)]
    // public virtual bool EnableEmailHyperlinks
    // {
    //     get => enableEmailHyperlinks;
    //     set
    //     {
    //         if (enableEmailHyperlinks == value) return;
    //         enableEmailHyperlinks = value;
    //         OnPropertyChanged("EnableEMailHyperlinks");
    //     }
    // }
    //
    // private bool requireControlModifierForHyperlinkClick = true;
    //
    // [DefaultValue(true)]
    // public virtual bool RequireControlModifierForHyperlinkClick
    // {
    //     get => requireControlModifierForHyperlinkClick;
    //     set
    //     {
    //         if (requireControlModifierForHyperlinkClick == value) return;
    //         requireControlModifierForHyperlinkClick = value;
    //         OnPropertyChanged("RequireControlModifierForHyperlinkClick");
    //     }
    // }

    #endregion

    #region TabSize / IndentationSize / ConvertTabsToSpaces / GetIndentationString

    private int indentationSize = 4;

    [DefaultValue(4)]
    public virtual int IndentationSize
    {
        get => indentationSize;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "value must be positive");
            if (value > 1000) throw new ArgumentOutOfRangeException(nameof(value), value, "indentation size is too large");
            if (indentationSize == value) return;
            indentationSize = value;
            OnPropertyChanged("IndentationSize");
            OnPropertyChanged("IndentationString");
        }
    }

    private bool convertTabsToSpaces;

    [DefaultValue(false)]
    public virtual bool ConvertTabsToSpaces
    {
        get => convertTabsToSpaces;
        set
        {
            if (convertTabsToSpaces == value) return;
            convertTabsToSpaces = value;
            OnPropertyChanged("ConvertTabsToSpaces");
            OnPropertyChanged("IndentationString");
        }
    }

    [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
    [Browsable(false)]
    public string IndentationString => GetIndentationString(1);

    public virtual string GetIndentationString(int column)
    {
        if (column < 1) throw new ArgumentOutOfRangeException(nameof(column), column, "Value must be at least 1.");
        var _indentationSize = IndentationSize;
        return ConvertTabsToSpaces ? new string(' ', _indentationSize - (column - 1) % _indentationSize) : "\t";
    }

    #endregion

    private bool cutCopyWholeLine = true;

    [DefaultValue(true)]
    public virtual bool CutCopyWholeLine
    {
        get => cutCopyWholeLine;
        set
        {
            if (cutCopyWholeLine == value) return;
            cutCopyWholeLine = value;
            OnPropertyChanged("CutCopyWholeLine");
        }
    }

    private bool allowScrollBelowDocument;

    [DefaultValue(false)]
    public virtual bool AllowScrollBelowDocument
    {
        get => allowScrollBelowDocument;
        set
        {
            if (allowScrollBelowDocument == value) return;
            allowScrollBelowDocument = value;
            OnPropertyChanged("AllowScrollBelowDocument");
        }
    }

    private double wordWrapIndentation;

    [DefaultValue(10.0)]
    public virtual double WordWrapIndentation
    {
        get => wordWrapIndentation;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "value must not be NaN/infinity");
            if (value == wordWrapIndentation) return;
            wordWrapIndentation = value;
            OnPropertyChanged("WordWrapIndentation");
        }
    }

    private bool inheritWordWrapIndentation = true;

    [DefaultValue(true)]
    public virtual bool InheritWordWrapIndentation
    {
        get => inheritWordWrapIndentation;
        set
        {
            if (value == inheritWordWrapIndentation) return;
            inheritWordWrapIndentation = value;
            OnPropertyChanged("InheritWordWrapIndentation");
        }
    }

    private bool enableRectangularSelection = true;

    [DefaultValue(true)]
    public bool EnableRectangularSelection
    {
        get => enableRectangularSelection;
        set
        {
            if (enableRectangularSelection == value) return;
            enableRectangularSelection = value;
            OnPropertyChanged("EnableRectangularSelection");
        }
    }

    private bool enableTextDragDrop = true;

    [DefaultValue(true)]
    public bool EnableTextDragDrop
    {
        get => enableTextDragDrop;
        set
        {
            if (enableTextDragDrop == value) return;
            enableTextDragDrop = value;
            OnPropertyChanged("EnableTextDragDrop");
        }
    }

    private bool enableVirtualSpace;

    [DefaultValue(false)]
    public virtual bool EnableVirtualSpace
    {
        get => enableVirtualSpace;
        set
        {
            if (enableVirtualSpace == value) return;
            enableVirtualSpace = value;
            OnPropertyChanged("EnableVirtualSpace");
        }
    }

    private bool enableImeSupport = true;

    [DefaultValue(true)]
    public virtual bool EnableImeSupport
    {
        get => enableImeSupport;
        set
        {
            if (enableImeSupport == value) return;
            enableImeSupport = value;
            OnPropertyChanged("EnableImeSupport");
        }
    }

    private bool showColumnRuler;

    [DefaultValue(false)]
    public virtual bool ShowColumnRuler
    {
        get => showColumnRuler;
        set
        {
            if (showColumnRuler == value) return;
            showColumnRuler = value;
            OnPropertyChanged("ShowColumnRuler");
        }
    }

    private int columnRulerPosition = 80;

    [DefaultValue(80)]
    public virtual int ColumnRulerPosition
    {
        get => columnRulerPosition;
        set
        {
            if (columnRulerPosition == value) return;
            columnRulerPosition = value;
            OnPropertyChanged("ColumnRulerPosition");
        }
    }

    private bool highlightCurrentLine;

    [DefaultValue(false)]
    public virtual bool HighlightCurrentLine
    {
        get => highlightCurrentLine;
        set
        {
            if (highlightCurrentLine == value) return;
            highlightCurrentLine = value;
            OnPropertyChanged("HighlightCurrentLine");
        }
    }

    private bool hideCursorWhileTyping = true;

    [DefaultValue(true)]
    public bool HideCursorWhileTyping
    {
        get => hideCursorWhileTyping;
        set
        {
            if (hideCursorWhileTyping == value) return;
            hideCursorWhileTyping = value;
            OnPropertyChanged("HideCursorWhileTyping");
        }
    }

    //private bool allowToggleOverstrikeMode;

    // [DefaultValue(false)]
    // public bool AllowToggleOverstrikeMode
    // {
    //     get => allowToggleOverstrikeMode;
    //     set
    //     {
    //         if (allowToggleOverstrikeMode == value) return;
    //         allowToggleOverstrikeMode = value;
    //         OnPropertyChanged("AllowToggleOverstrikeMode");
    //     }
    // }
}