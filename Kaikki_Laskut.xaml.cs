using System.Windows;
using System.Windows.Controls;
using static Harjoitustyö.Uusi_Lasku;

namespace Harjoitustyö
{
    /// <summary>
    /// Interaction logic for Kaikki_Laskut.xaml
    /// </summary>
    public partial class Kaikki_Laskut : Window
    {
        public Kaikki_Laskut()
        {
            InitializeComponent();
            var laskut = Tietokanta.HaeKaikkiLaskut();
            TuoteLista.ItemsSource = laskut;
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko valikko = new Päävalikko();
            valikko.Show();
            this.Close();
        }

        private void OpenPDF_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                TextBlock textBlock = (TextBlock)sender;
                // Tämä olio on peräisin listanäkymästä, eikä siinä ole tuoterivejä mukana
                Lasku listaltaValittu = (Lasku)textBlock.DataContext;

                // Määritetään tallennuspolku
                string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
                string folderPath = System.IO.Path.Combine(projectRoot, "Laskut");

                // Varmistetaan, että kansio on olemassa
                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);

                string fileName = $"Lasku_{listaltaValittu.LaskunNumero}.pdf";
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                // KORJAUS ALKAA TÄSTÄ:
                // Haetaan laskun TÄYDELLISET tiedot tietokannasta numeron perusteella
                Lasku täysiLasku = Tietokanta.HaeLasku(listaltaValittu.LaskunNumero);

                if (täysiLasku != null)
                {
                    // Haetaan erikseen laskun tuoterivit, jotta taulukko ei ole tyhjä
                    täysiLasku.Tuotteet = Tietokanta.HaeTuotteetLaskulle(täysiLasku.LaskunNumero);

                    // Luodaan PDF käyttäen täydellistä dataa. 
                    // Tämä ylikirjoittaa mahdolliset aiemmat tyhjät PDF-tiedostot.
                    PdfService.LuoPDF(täysiLasku, fullPath);
                }

                // Avataan valmis PDF-tiedosto oletusohjelmalla
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe PDF:n avaamisessa tai luomisessa: {ex.Message}");
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DeleteID.Text, out int id))
            {
                // Kysytään vahvistus käyttäjältä
                MessageBoxResult result = MessageBox.Show($"Haluatko varmasti poistaa laskun numero {id}?",
                    "Vahvista poisto", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Kutsutaan tietokantafunktiota
                    bool onnistui = Tietokanta.PoistaLasku(id);

                    if (onnistui)
                    {
                        MessageBox.Show("Lasku poistettu onnistuneesti.");
                        DeleteID.Text = ""; // Tyhjennetään tekstikenttä

                        // Päivitetään lista hakemalla laskut uudelleen
                        var laskut = Tietokanta.HaeKaikkiLaskut();
                        TuoteLista.ItemsSource = laskut;
                    }
                    else
                    {
                        MessageBox.Show("Laskun poisto epäonnistui. Tarkista ID.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen laskun numero.");
            }
        }
    }
}
