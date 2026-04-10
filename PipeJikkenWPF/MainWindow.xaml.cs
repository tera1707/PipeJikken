using PipeJikken;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

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


namespace PipeJikkenWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    PipeServer? _pipeServer;
    PipeClient? _pipeClient;

    public MainWindow()
    {
        InitializeComponent();
    }

    // サーバー起動
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (_pipeServer is not null)
        {
            Debug.WriteLine($"すでにパイプサーバーが作成されています。");
            return;
        }

        this.Title = "Server.";

        // 複数ユーザーがログインしているときに、全く同じ名前のパイプを作って衝突しないようにセッションIDをパイプ名に付ける
        //var id = Process.GetCurrentProcess().SessionId;

        _pipeServer = new PipeServer();
        _pipeServer.Create(PipeNameTextBox.Text);

        var recvTask = _pipeServer.StartAsync(data =>
        {
            // クライアントから受信した文言
            this.Dispatcher.Invoke(async () =>
            {
                if (ResponseCheck.IsChecked == true)
                {
                    // 応答を送信
                    await _pipeServer.SendString("Ack");
                }

                DataList.Items.Insert(0, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + data);                 
            });
        });

        // 受信スレッドは、dispose時にキャンセルされる
        recvTask.ContinueWith(task =>
        {
            Debug.WriteLine($"task.Status = {task.Status}" ); // →status＝Cancelのはず
        });
    }

    // サーバー終了
    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        _pipeServer?.Dispose();
        _pipeServer = null;
    }

    // クライアントを作成
    private async void Button_Click_2(object sender, RoutedEventArgs e)
    {
        this.Title = "Client.";

        //var id = Process.GetCurrentProcess().SessionId;

        _pipeClient = new PipeClient();
        await _pipeClient.Create(PipeNameTextBox.Text);
    }

    // クライアントで送信
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        if (_pipeClient is null)
        {
            Debug.WriteLine($"パイプクライアントを作成してください。");
            return;
        }

        var send = SendData.Text;
        _ = _pipeClient.SendAsync(send, (response) =>
        {
            // サーバーからの応答文言
            this.Dispatcher.Invoke(() => {
                DataList.Items.Insert(0, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + response);
            });
        });
    }

    // クライアント終了
    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
        if (_pipeClient is null)
        {
            Debug.WriteLine($"すでにパイプクライアントは破棄されています。");
            return;
        }

        _pipeClient?.Dispose();
        _pipeClient = null;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_pipeServer != null)
        {
            _pipeServer.Dispose();
            _pipeServer = null;
        }
        if (_pipeClient != null)
        {
            _pipeClient.Dispose();
            _pipeClient = null;
        }
    }
}
