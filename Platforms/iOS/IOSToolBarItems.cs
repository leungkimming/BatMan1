using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Microsoft.Maui.Handlers;
using System.Drawing;
using UIKit;

namespace BatMan2;
public class IOSContentPageHandler : PageHandler {
    ContentPage? ContentPage => VirtualView as ContentPage;
    UIViewController? UIVC;
    public static SecondaryToolbarSettings SecondaryToolbarUserSettings { get; set; } = new SecondaryToolbarSettings();

    private bool isDropDownMenuActive;
    private ToolbarItem[] secondaryToolbarItems;
    private UIView? transparentCloseDropDownMenuView;
    private UITapGestureRecognizer? transparentCloseDropDownMenuViewTapRecognizer;
    private UITableView? toolbarTableView;
    private ToolbarItem? dropdownMenuToolbarItem;
    public IOSContentPageHandler() {
        secondaryToolbarItems = Array.Empty<ToolbarItem>();
    }
    protected override void ConnectHandler(Microsoft.Maui.Platform.ContentView nativeView) {
        base.ConnectHandler(nativeView);
        SetupPageToolbarMenu();
        ContentPage.Loaded += OnLoaded;
    }

    protected override void DisconnectHandler(Microsoft.Maui.Platform.ContentView nativeView) {
        ContentPage.Loaded -= OnLoaded;
        RestoreToolbarMenu();
        base.DisconnectHandler(nativeView);
    }
    public class SecondaryToolbarSettings {
        public string Icon { get; set; } = "more.png";

        public float RowHeight { get; set; } = -1;

        public float ColumnWidth { get; set; } = 200.0f;

        //public float CornerRadius { get; set; } = 5.0f;

        public float ShadowOpacity { get; set; } = 0.7f;

        public float ShadowRadius { get; set; } = 4.0f;

        public float ShadowOffsetX { get; set; } = 0.0f;

        public float ShadowOffsetY { get; set; } = 0.0f;

        public UIFont Font { get; set; } = UIFont.PreferredCallout;

        public float GetRowHeight() {
            return SecondaryToolbarUserSettings.RowHeight > 0 ?
                SecondaryToolbarUserSettings.RowHeight :
                (float)(this.Font.Ascender - this.Font.Descender) * 2;
        }
    }
    private void RestoreToolbarMenu() {
        if (this.secondaryToolbarItems != null) {
            foreach (var toolbarItem in this.secondaryToolbarItems) {
                ContentPage.ToolbarItems.Add(toolbarItem);
            }
        }
        if (this.dropdownMenuToolbarItem != null) {
            ContentPage.ToolbarItems.Remove(this.dropdownMenuToolbarItem);
            this.dropdownMenuToolbarItem = null; // Clear the reference
        }
    }

    private void SetupPageToolbarMenu() {
        IPlatformViewHandler handler = this;
        UIVC = handler.ViewController;

        if (ContentPage!.ToolbarItems.Any(i => i.Order == ToolbarItemOrder.Secondary)) {
            this.secondaryToolbarItems = ContentPage.ToolbarItems.Where(i => i.Order == ToolbarItemOrder.Secondary).OrderBy(b => b.Priority).ToArray();
            foreach (var toolbarItem in this.secondaryToolbarItems) {
                var commandBak = toolbarItem.Command;
                ContentPage.ToolbarItems.Remove(toolbarItem);
                toolbarItem.Command = commandBak;
            }
            this.dropdownMenuToolbarItem = new ToolbarItem {
                Order = ToolbarItemOrder.Primary,
                Priority = int.MaxValue,
                IconImageSource = SecondaryToolbarUserSettings.Icon,
                Command = new Command(this.ToggleDropDownMenu)
            };
            ContentPage.ToolbarItems.Add(this.dropdownMenuToolbarItem);
        }
    }
    private void OnLoaded(object? sender, EventArgs? e) {
        IPlatformViewHandler handler = this;
        UIVC = handler.ViewController;
    }
    private void OpenDropDownMenu() {
        if (!this.isDropDownMenuActive && secondaryToolbarItems.Length > 0) {
            this.isDropDownMenuActive = true;

            this.transparentCloseDropDownMenuView = new UIView(new CGRect(0, 0, UIVC!.View!.Bounds.Width, UIVC.View.Bounds.Height)) {
                BackgroundColor = UIColor.FromRGBA(0, 0, 0, 0)
            };
            this.transparentCloseDropDownMenuViewTapRecognizer = new UITapGestureRecognizer(this.CloseDropDownMenu);
            this.transparentCloseDropDownMenuView.AddGestureRecognizer(transparentCloseDropDownMenuViewTapRecognizer);
            UIVC.Add(this.transparentCloseDropDownMenuView);
            this.toolbarTableView = new UITableView(this.GetPositionForDropDownMenu()) {
                Source = new TableSource(this.secondaryToolbarItems, this.CloseDropDownMenu),
                ClipsToBounds = true,
                ScrollEnabled = false,
                //BackgroundColor = UserSettings.MenuBackgroundColor.ToUIColor()
            };
            //this.toolbarTableView.Layer.CornerRadius = RightToolbarMenuUserSettings.CornerRadius;

            this.toolbarTableView.Layer.MasksToBounds = false;
            //this.toolbarTableView.Layer.ShadowColor = UserSettings.ShadowColor.ToCGColor();
            this.toolbarTableView.Layer.ShadowOpacity = SecondaryToolbarUserSettings.ShadowOpacity;
            this.toolbarTableView.Layer.ShadowRadius = SecondaryToolbarUserSettings.ShadowRadius;
            this.toolbarTableView.Layer.ShadowOffset = new Microsoft.Maui.Graphics.SizeF(SecondaryToolbarUserSettings.ShadowOffsetX, SecondaryToolbarUserSettings.ShadowOffsetY);

            UIVC.Add(this.toolbarTableView);
        }
    }

    private void CloseDropDownMenu() {
        if (this.isDropDownMenuActive) {
            this.isDropDownMenuActive = false;

            this.transparentCloseDropDownMenuView?.RemoveGestureRecognizer(transparentCloseDropDownMenuViewTapRecognizer!);
            this.transparentCloseDropDownMenuViewTapRecognizer?.Dispose();
            this.transparentCloseDropDownMenuViewTapRecognizer = null;

            this.toolbarTableView?.RemoveFromSuperview();
            this.toolbarTableView?.Dispose();
            this.toolbarTableView = null;

            this.transparentCloseDropDownMenuView?.RemoveFromSuperview();
            this.transparentCloseDropDownMenuView?.Dispose();
            this.transparentCloseDropDownMenuView = null;
        }
    }

    private void ToggleDropDownMenu() {
        if (!this.isDropDownMenuActive)
            this.OpenDropDownMenu();
        else
            this.CloseDropDownMenu();
    }

    private RectangleF GetPositionForDropDownMenu() {
        return new RectangleF(
            (float)UIVC.View.Bounds.Width - SecondaryToolbarUserSettings.ColumnWidth,
            0,
            SecondaryToolbarUserSettings.ColumnWidth,
            this.secondaryToolbarItems.Length * SecondaryToolbarUserSettings.GetRowHeight());
    }
}

internal class TableSource : UITableViewSource {
    private ToolbarItem[] toolbarItems { get; set; }
    private Action itemSelected { get; set; }

    private const string CellIdentifier = "RightToolbarTableCell";

    public TableSource(ToolbarItem[] toolbarItems, Action itemSelected) {
        this.toolbarItems = toolbarItems;
        this.itemSelected = itemSelected;
    }

    public override nint RowsInSection(UITableView tableView, nint section) {
        return this.toolbarItems.Length;
    }

    public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) {
        return IOSContentPageHandler.SecondaryToolbarUserSettings.GetRowHeight();
    }

    public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath) {
        var toolbarItem = this.toolbarItems[indexPath.Row];

        UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
        if (cell == null) {
            cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier);
        }
        //var cell = tableView.DequeueReusableCell(CellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier);
        var contentConfiguration = UIListContentConfiguration.CellConfiguration;
        contentConfiguration.Text = toolbarItem.Text;
        contentConfiguration.TextProperties.Font = IOSContentPageHandler.SecondaryToolbarUserSettings.Font;
        contentConfiguration.ImageProperties.MaximumSize = new CGSize(24, 24); //3 dots image size

        UIColor textColor = toolbarItem.IsEnabled ? UIColor.Label : UIColor.SystemGray2;
        contentConfiguration.TextProperties.Color = textColor;

        if (toolbarItem.IconImageSource != null) {
            var imageSourceHandler = GetImageSourceHandler(toolbarItem.IconImageSource);
            if (imageSourceHandler != null) {
                var imageLoadTask = imageSourceHandler.LoadImageAsync(toolbarItem.IconImageSource);
                imageLoadTask.ContinueWith(task => {
                    if (task.IsCompletedSuccessfully) {
                        var image = task.Result;
                        InvokeOnMainThread(() => {
                            contentConfiguration.Image = image;
                            cell.ContentConfiguration = contentConfiguration;
                        });
                    }
                });
            }
        } else {
            cell.ContentConfiguration = contentConfiguration;
        }
        cell.UserInteractionEnabled = toolbarItem.IsEnabled;
        return cell;
    }

    public override void RowSelected(UITableView tableView, NSIndexPath indexPath) {
        var toolbarItem = this.toolbarItems[indexPath.Row];

        if (toolbarItem.Command != null && toolbarItem.Command.CanExecute(toolbarItem.CommandParameter))
            toolbarItem.Command.Execute(toolbarItem.CommandParameter);

        var onClickedMethodInfo = toolbarItem.GetType().GetMethod("OnClicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        onClickedMethodInfo!.Invoke(toolbarItem, new object[0]);

        tableView.DeselectRow(indexPath, true);

        this.itemSelected?.Invoke();
    }

    private static IImageSourceHandler? GetImageSourceHandler(ImageSource source) {
        if (source is UriImageSource)
            return new ImageLoaderSourceHandler();
        if (source is FileImageSource)
            return new FileImageSourceHandler();
        if (source is StreamImageSource)
            return new StreamImagesourceHandler();
        return null;
    }
}

