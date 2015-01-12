using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace Mwman.Views
{
    /// <summary>
    /// Interaction logic for AddLinkView.xaml
    /// </summary>
    public partial class AddLinkView : Window
    {
        public AddLinkView()
        {
            InitializeComponent();
            KeyDown += AddLinkView_KeyDown;
        }

        void AddLinkView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyDown -= AddLinkView_KeyDown;
                Close();
            }
            if (e.Key == Key.Enter)
            {
                //нажмем кнопку программно
                var peer = new ButtonAutomationPeer(ButtonGo);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                if (invokeProv != null)
                    invokeProv.Invoke();
            }
        }

        private void AddLinkView_OnLoaded(object sender, RoutedEventArgs e)
        {
            TextBoxLink.SelectAll();
        }

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            CheckBoxAud.IsChecked = !CheckBoxAud.IsChecked;
        }
    }
}
