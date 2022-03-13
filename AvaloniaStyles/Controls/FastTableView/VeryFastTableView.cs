using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView : Control, IKeyboardNavigationHandler
{
    private static double ColumnSpacing = 10;
    private static double RowHeight = 28;
    private static double DrawingStartOffsetY = RowHeight;
    private static SolidColorBrush OddRowBackground = new SolidColorBrush(Colors.White);
    private static SolidColorBrush SelectedRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    private static SolidColorBrush EvenRowBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    private static SolidColorBrush HeaderBackground = new SolidColorBrush(Colors.White);
    private static IPen HeaderBorderBackground = new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 1);
    private static IPen BorderPen = new Pen(new SolidColorBrush(Colors.Black), 1);
    private static IPen FocusPen = new Pen(new SolidColorBrush(Colors.Black), 2);
    private static IPen FocusOuterPen = new Pen(new SolidColorBrush(Colors.White), 3);
    private static SolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
    private static SolidColorBrush FocusTextBrush = OddRowBackground;

    public int ColumnsCount => Columns?.Count ?? 0;

    private PhantomTextBox editor = new();

    static VeryFastTableView()
    {
        RowsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.Rebind(e.OldValue as IReadOnlyList<ITableRow>, e.NewValue as IReadOnlyList<ITableRow>);
        });
        HiddenColumnsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.RebindHiddenColumns(e.OldValue as IReadOnlyList<int>, e.NewValue as IReadOnlyList<int>);
        });
        ColumnsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            var list = e.NewValue as IReadOnlyList<ITableColumnHeader>;
            while (list != null && view.columnVisibility.Count < list.Count)
                view.columnVisibility.Add(true);
        });
        SelectedRowIndexProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.EnsureRowVisible((int)e.NewValue!);
        });
        SelectedCellIndexProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.EnsureCellVisible((int)e.NewValue!);
        });
        AffectsRender<VeryFastTableView>(SelectedRowIndexProperty);
        AffectsRender<VeryFastTableView>(SelectedCellIndexProperty);
        FocusableProperty.OverrideDefaultValue<VeryFastTableView>(true);
    }

    public VeryFastTableView()
    {
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() =>
            {
                if (IsSelectedCellValid)
                    OpenEditor();
            })
        });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        if (e.Key == Key.Down)
        {
            e.Handled = MoveCursorDown();
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = MoveCursorUp();
        }
        else if (e.Key == Key.Left)
        {
            e.Handled = MoveCursorLeft();
        }
        else if (e.Key == Key.Right)
        {
            e.Handled = MoveCursorRight();
        }
        else if (e.Key == Key.Tab)
        {
            e.Handled = (e.KeyModifiers & KeyModifiers.Shift) != 0 ? MoveCursorPrevious() : MoveCursorNext();
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled)
            return;
        
        if (IsSelectedCellValid)
        {
            OpenEditor(e.Text);
            e.Handled = true;
        }
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        Cursor = Cursor.Default;
        base.OnPointerLeave(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Cursor = (currentlyResizedColumn.HasValue || IsPointHeader(e.GetPosition(this))) ? new Cursor(StandardCursorType.SizeWestEast) : Cursor.Default;

        if (CustomCellDrawer != null)
        {
            var point = e.GetCurrentPoint(this);
            if (CustomCellDrawer.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed))
                InvalidateVisual();
        }

        if (currentlyResizedColumn.HasValue && Columns != null)
        {
            Columns[currentlyResizedColumn.Value].Width = Math.Max(5, resizingColumnStartWidth + e.GetPosition(this).X - resizingColumnStartPointerX);
            //todo: this should observe width, but this might be good enough for now
            InvalidateVisual();
            InvalidateMeasure();
        }
    }

    private int? currentlyResizedColumn = null;
    private double resizingColumnStartWidth = 0;
    private double resizingColumnStartPointerX = 0;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsPointHeader(e.GetPosition(this)))
        {
            // row
            {
                var index = GetRowIndexByY(e.GetPosition(this).Y);
                if (Rows == null || Rows.Count == 0 || !index.HasValue)
                    SelectedRowIndex = -1;
                else
                    SelectedRowIndex = Math.Clamp(index.Value, 0, Rows!.Count - 1);
            }
            // cell
            {
                var index = GetColumnIndexByX(e.GetPosition(this).X);
                if (Rows?.Count == 0 || !index.HasValue)
                    SelectedCellIndex = -1;
                else
                    SelectedCellIndex = Math.Clamp(index.Value, 0, ColumnsCount - 1);
            }
        }
        else
        {
            currentlyResizedColumn = GetColumnIndexByX(e.GetPosition(this).X);
            if (currentlyResizedColumn.HasValue)
            {
                resizingColumnStartWidth = GetColumnRect(currentlyResizedColumn.Value).width;
                resizingColumnStartPointerX = e.GetPosition(this).X;
            }
        }

        if (IsSelectedCellValid && Rows != null)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            var cell = Rows[SelectedRowIndex].CellsList[SelectedCellIndex];
            if (CustomCellInteractor != null && CustomCellInteractor.PointerDown(cell,
                    properties.IsLeftButtonPressed, 
                    properties.IsRightButtonPressed, 
                    e.ClickCount))
            {
                e.Handled = true;
            }
            
            if (!e.Handled)
            {
                if (e.ClickCount == 2 && properties.IsLeftButtonPressed)
                {
                    OpenEditor();
                    e.Handled = true;
                }
            }
        }
        
        if (CustomCellDrawer != null)
        {
            var point = e.GetCurrentPoint(this);
            if (CustomCellDrawer.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed))
                InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (currentlyResizedColumn.HasValue)
        {
            currentlyResizedColumn = null;
            e.Handled = true;
            return;
        }

        if (IsSelectedCellValid && Rows != null)
        {
            var cell = Rows[SelectedRowIndex].CellsList[SelectedCellIndex];
            if (CustomCellInteractor != null && CustomCellInteractor.PointerUp(cell,
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased,
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased))
            {
                e.Handled = true;
            }
        }
        
        if (CustomCellDrawer != null)
        {
            var point = e.GetCurrentPoint(this);
            if (CustomCellDrawer.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed))
                InvalidateVisual();
        }
    }

    private void OpenEditor(string? customText = null)
    {
        if (Rows == null)
            return;
        if (CustomCellInteractor == null ||
            !CustomCellInteractor.SpawnEditorFor(customText, this, SelectedCellRect, Rows[SelectedRowIndex].CellsList[SelectedCellIndex]))
        {
            editor.Spawn(this, SelectedCellRect, customText ?? Rows[SelectedRowIndex].CellsList[SelectedCellIndex].ToString()!, customText == null, text =>
            {
                Rows[SelectedRowIndex].CellsList[SelectedCellIndex].UpdateFromString(text);
            });   
        }
    }
    
    public bool IsSelectedCellValid => SelectedCellIndex >= 0 && SelectedCellIndex < ColumnsCount && SelectedRowIndex >= 0 && SelectedRowIndex < Rows?.Count;
    public Rect SelectedCellRect
    {
        get
        {
            var widthRect = GetColumnRect(SelectedCellIndex);
            return new Rect(widthRect.x, SelectedRowIndex * RowHeight + DrawingStartOffsetY, widthRect.width, RowHeight);
        }
    }

    private ScrollViewer ScrollViewer => this.FindAncestorOfType<ScrollViewer>();


    private bool IsRowVisible(int row, ScrollViewer? scroll = null)
    {
        if (Rows == null)
            return false;
        if (scroll == null)
            scroll = ScrollViewer;
        var startIndex = Math.Max(0, (int)(scroll.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scroll.Viewport.Height / RowHeight + 2, Rows.Count);
        return row >= startIndex && row < endIndex;
    }
}