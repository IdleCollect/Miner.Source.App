using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crypto.Earn.App.Frontend.WPF.Controls.Installation; 

public partial class AccessInformationControl : UserControl {

    public bool HasAccess { get; set; } = true;
    public string TitleText { get; set; }
    public string SubText { get; set; }
    
    public AccessInformationControl() {
        InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if(!HasAccess)  {
            var noAccessBrush = new SolidColorBrush(Colors.Gray);
            EllipseInner.Fill = noAccessBrush;
            EllipseOuter.Fill = noAccessBrush;
            Title.Foreground  = noAccessBrush;
        }

        Title.Content = TitleText;
        Subtext.Content = SubText;
    }
}