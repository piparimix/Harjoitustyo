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

        private void AvaaPDFRivi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Haetaan painettu nappi ja sen takana oleva lasku
                if (sender is Button button && button.DataContext is Lasku listaltaValittu)
                {
                    // Määritellään polku Laskut-kansioon
                    string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
                    string folderPath = System.IO.Path.Combine(projectRoot, "Laskut");

                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }

                    string fileName = $"Lasku_{listaltaValittu.LaskunNumero}.pdf";
                    string fullPath = System.IO.Path.Combine(folderPath, fileName);

                    // 2. Haetaan laskun TÄYDELLISET tiedot tietokannasta (sis. tuoterivit)
                    // Koska listanäkymän oliossa ei välttämättä ole tuoterivejä ladattuna
                    Lasku täysiLasku = Tietokanta.HaeLasku(listaltaValittu.LaskunNumero);

                    if (täysiLasku != null)
                    {
                        // Varmistetaan rivit
                        täysiLasku.Tuotteet = Tietokanta.HaeTuotteetLaskulle(täysiLasku.LaskunNumero);

                        // Luodaan PDF
                        PdfService.LuoPDF(täysiLasku, fullPath);

                        // Avataan PDF
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Laskun tietojen haku epäonnistui.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe PDF:n luonnissa: {ex.Message}");
            }
        }

        private void PoistaLaskuRivi_Click(object sender, RoutedEventArgs e)
        {
            // MUUTOS 1: Tarkistetaan, että rivi on tyyppiä 'Lasku', ei 'Tuote'
            if (sender is Button button && button.DataContext is Lasku valittuLasku)
            {
                // MUUTOS 2: Laskulla ID on 'LaskunNumero', ei 'Tuote_ID'
                int id = valittuLasku.LaskunNumero;

                MessageBoxResult result = MessageBox.Show(
                    $"Haluatko varmasti poistaa laskun numero {id}?\n\nTämä poistaa laskun pysyvästi.",
                    "Vahvista poisto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // MUUTOS 3: Kutsutaan laskun poistoa
                        bool onnistui = Tietokanta.PoistaLasku(id);

                        if (onnistui)
                        {
                            MessageBox.Show("Lasku poistettu onnistuneesti.");

                            // Päivitetään lista (DataGridin nimi on koodissasi 'TuoteLista' vaikka siinä on laskuja)
                            TuoteLista.ItemsSource = Tietokanta.HaeKaikkiLaskut();
                        }
                        else
                        {
                            MessageBox.Show("Poisto epäonnistui.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Virhe: {ex.Message}");
                    }
                }
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
