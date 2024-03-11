using PipeJikken;
using System.Diagnostics;
using System.Windows;

// ■使い方
// 
// ・サーバー用アプリ起動(①)、クライアント用アプリ起動(②)
// ・①で「サーバー起動」を押して、名前付きパイプサーバーを立ち上げる
// ・②で「クライアント作成」を押して、名前付きパイプサーバーにクライアントを接続しに行く
// ・②で「クライアントで送信」を押して、データ送る
// ・あとは、「クライアントで送信」を押すと、連続してデータを送れる
// 
// ・②を閉じると、①側も一度パイプサーバーを終了する
// ・もう一度接続する際は、再度①の「サーバー起動」を押して、名前付きパイプサーバーを立ち上げる。あとは上と同じ。
// 


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

            _pc.CreatePipeServer(@"_pipename_" + id);

            _pc?.StartServerAsync((data =>
            {
                // 応答を送信
                _pc?.SendString("aiueo");

                // クライアントから受信した文言
                this.Dispatcher.Invoke(() => { DataList.Items.Add(data); });
            }));

        }

        // クライアントを作成
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var id = Process.GetCurrentProcess().SessionId;

            await _pc!.CreatePipeClient(@"_pipename_" + id);

        }

        // クライアントで送信
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var send = SendData.Text;
            _pc?.StartClientAsync(send, (response) =>
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
