
namespace PSSsh.Logs
{
    internal enum LogLevel
    {
        Debug = -1,     //  デバッグレベル
        Info = 0,       //  通常の情報レベル。
        Attention = 1,  //  正常な結果に基づく終了の場合に使用
        Warn = 2,       //  警告レベル。軽度な問題。パラメータ不足、等
        Error = 3,      //  エラーレベル。重度の問題。予期しない問題、等
    }
}
