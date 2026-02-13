using System.Globalization;
using System.IO;
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

            // Poistetaan vanhat laskutiedostot ja tietokanta, jotta sovellus alkaa puhtaalta pöydältä joka kerta.  
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
            string folderPath = Path.Combine(projectRoot, "Laskut");

            // Tarkistetaan onko kansio olemassa ja poistetaan se sisältöineen (true = rekursiivinen poisto)
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (Exception ex)
                {
                    
                    System.Diagnostics.Debug.WriteLine("Kansion poisto epäonnistui: " + ex.Message);
                }
            }
            Harjoitustyö.Tietokanta.PoistaTietokanta();
        }
    }
}
