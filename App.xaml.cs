using System.Globalization;
using System.Windows;
using System.Windows.Markup;


namespace Harjoitustyö
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        // vaihdetaan sovelluksen käynnistyessä kulttuuri suomeksi
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var culture = new CultureInfo("fi-FI");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            Harjoitustyö.Tietokanta.PoistaTietokanta();
        }
    }
}
