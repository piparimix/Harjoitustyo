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

        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // vaihdetaan sovelluksen käynnistyessä kulttuuri suomeksi
            var culture = new CultureInfo("fi-FI");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Määritetään sovelluksen kieli suomeksi, jotta esimerkiksi päivämäärät ja numerot näytetään suomalaiseen tapaan.
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            // Määritetään polku Laskut-kansioon, joka sijaitsee projektin juurihakemistossa. 
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
            string folderPath = Path.Combine(projectRoot, "Laskut");

            // Tarkistetaan onko Laskut kansio olemassa ja poistetaan se sisältöineen
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

            // Poistetaan vanha tietokanta, jotta sovellus alkaa puhtaalta pöydältä joka kerta. itse metodi on määritetty Tietokanta-luokassa, joka löytyy Tietokanta.cs-tiedostosta.
            Harjoitustyö.Tietokanta.PoistaTietokanta();

            // Alustetaan tietokanta. itse metodi on määritetty Tietokanta-luokassa, joka löytyy Tietokanta.cs-tiedostosta.
            Tietokanta.AlustaTietokanta();
        }
    }
}
