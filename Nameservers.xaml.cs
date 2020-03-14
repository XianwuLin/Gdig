using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gdig
{
    /// <summary>
    /// Nameservers.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class Nameservers : Window
    {
        public class NameServer
        {
            public string Name { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public override string ToString()
            {
                return String.Format("{0} -> {1}:{2}", Name, Host, Port);
            }
        }

        public List<NameServer> ns;
        public Nameservers()
        {
            InitializeComponent();

            List<string> items = new List<string>(((string)Properties.Settings.Default["nameServers"]).Split(new char[] { ';' }));
            string dns_server_name, dns_server_ip;
            int dns_server_port;
            ns = new List<NameServer>();
            foreach (var item in items) {
                string[] dns_server_conf = Regex.Split(item, "->|:", RegexOptions.IgnoreCase);
                dns_server_name = dns_server_conf[0].Trim();
                dns_server_ip = dns_server_conf[1].Trim();
                dns_server_port = Int32.Parse(dns_server_conf[2].Trim());
                ns.Add(new NameServer()
                {
                    Name = dns_server_name,
                    Host = dns_server_ip,
                    Port = dns_server_port
                });
            }

            dg_ns.ItemsSource = ns;
        }

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            List<string> res_list = new List<string>();
            foreach (var item in ns) {
                res_list.Add(item.ToString());
            }
            Properties.Settings.Default["nameServers"] = string.Join(";", res_list);
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
