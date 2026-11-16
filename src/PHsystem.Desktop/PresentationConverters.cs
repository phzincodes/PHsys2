using System.Globalization;
using System.Windows.Data;

namespace PHsystem.Desktop;

public sealed class ReactionSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is PHsystem.SharedContracts.V1.PostDto post
        ? post.Likes is { } likes ? $"{likes:N0} curtidas" : $"{post.Score:N0} votos" : "Contagem indisponível";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ByteSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is long bytes ? DisplayFormat.Bytes(bytes) : "—";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
public sealed class ReviewLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value as string) switch
    {
        "Removed" => "Indisponível", "Unverified" => "Não verificada", "TechnicallyReviewed" => "Revisão técnica", _ => "Estado não informado"
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ReplyIndentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new System.Windows.Thickness(value is int depth ? Math.Clamp(depth, 0, 4) * 18 : 0, 4, 0, 4);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
public sealed class NotificationLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value as string) switch
    { "NewReply" => "Nova resposta na sua publicação", "AcceptedSolution" => "Sua resposta foi aceita como solução", _ => "Atualização da comunidade" };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
