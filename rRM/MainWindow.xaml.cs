using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PcapDotNet.Core;

namespace rRM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Rule> Rules { get; set; } 
        public Rule SelectedRule { get; set; }
        
        public List<Interface> Interfaces { get; set; }
        public Interface SelectedInteface { get; set; }
        
        public IEnumerable<Route> Routes { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();

            var windowsInterfaces = Interface.GetWindowsInterfaces();
            
            Interfaces = LivePacketDevice.AllLocalMachine
                .Select(device => Interface.TryParse(device, windowsInterfaces, out var @interface) ? @interface : null)
                .Where(@interface => @interface != null)
                .ToList();

            var indexedInterfaces = Interfaces.ToDictionary(i => i.Index, i => i);
            
            Routes = Route.Read(indexedInterfaces);
            Rules = Rule.LoadRules();

            InterfaceView.DataContext = this;
            RuleView.DataContext = this;

            Closing += (s, a) =>
            {
                Process.GetCurrentProcess().Kill();
            };
            
            // InterfaceView.ItemsSource = Interfaces;
            
            RoutesView.ItemsSource = Routes;

            CopyInterfaceName.Command = new DelegateCommand(CopySeletedInterfaceName);
        }

        public void CopySeletedInterfaceName()
        {
            Clipboard.SetText(SelectedInteface?.Name ?? "");
        }
        
        public class DelegateCommand : ICommand  
        {  
            public delegate void SimpleEventHandler();  
            private SimpleEventHandler handler;  
            private bool isEnabled = true;  
 
            public event EventHandler CanExecuteChanged;  
 
            public DelegateCommand(SimpleEventHandler handler)  
            {  
                this.handler = handler;  
            }  
 
            private void OnCanExecuteChanged()  
            {  
                if (this.CanExecuteChanged != null)  
                {  
                    this.CanExecuteChanged(this, EventArgs.Empty);  
                }  
            }  
 
            bool ICommand.CanExecute(object arg)  
            {  
                return this.IsEnabled;  
            }  
 
            void ICommand.Execute(object arg)  
            {  
                this.handler();  
            }  
 
            public bool IsEnabled  
            {  
                get { return this.isEnabled; }  
 
                set 
                {  
                    this.isEnabled = value;  
                    this.OnCanExecuteChanged();  
                }  
            }  
        }  
    }
}