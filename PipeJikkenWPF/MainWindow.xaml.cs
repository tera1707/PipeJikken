using PipeJikken;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PipeJikkenWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PipeConnect? _pc;

        public MainWindow()
        {
            InitializeComponent();

            _pc = new PipeConnect();
        }

        // サーバー起動
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var id = Process.GetCurrentProcess().SessionId;

            _pc?.CreateServerAsync(@"_pipename_" + id, (data =>
            {
                // クライアントから受信した文言
                this.Dispatcher.Invoke(() => { DataList.Items.Add(data); });
            }));
        }

        // クライアントで送信
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var id = Process.GetCurrentProcess().SessionId;

            var send = SendData.Text;
            _pc?.CreateClientAsync(@"_pipename_" + id, send, (response) =>
            {
                // サーバーからの応答文言
                this.Dispatcher.Invoke(() => { DataList.Items.Add(response); });
            });
            //_pc?.CreateClientAsync(@"_pipename_", send);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _pc?.Dispose();
        }
    }
}
