using System.Collections.ObjectModel;
using System.Windows;
using static Harjoitustyö.Uusi_Lasku;


namespace Harjoitustyö
{
    /// <summary>
    /// Interaction logic for Tuote_Lista.xaml
    /// </summary>
    public partial class Tuote_Lista : Window
    {
        public Tuote_Lista()
        {
            InitializeComponent();

            // Hae tuotteet tietokantaluokan avulla ja aseta ne taulukkoon
            var tuotteet = Tietokanta.HaeKaikkiTuotteet();
            TuoteLista.ItemsSource = tuotteet;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko myWindow = new Päävalikko();
            myWindow.Show();
            this.Close();
        }

        private void LataaTuotteet()
        {
            var tuotteet = Tietokanta.HaeKaikkiTuotteet();
            TuoteLista.ItemsSource = tuotteet;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var tuotteet = TuoteLista.ItemsSource as ObservableCollection<Lasku.Tuote>;

            if (tuotteet != null)
            {
                int paivitettyMaara = 0;

                foreach (var tuote in tuotteet)
                {
                    // Varmistetaan että on oikea ID ennen päivitystä
                    if (tuote.Tuote_ID > 0)
                    {
                        Tietokanta.PaivitaTuote(tuote);
                        paivitettyMaara++;
                    }
                }

                MessageBox.Show($"Muutokset tallennettu tietokantaan");         
                LataaTuotteet();
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            // Tarkistetaan, onko tekstikenttään syötetty numero
            if (int.TryParse(DeleteID.Text, out int id))
            {
                // Kysytään vahvistus käyttäjältä
                MessageBoxResult result = MessageBox.Show(
                    $"Haluatko varmasti poistaa tuoterivin ID: {id}?\n\nTämä poistaa tuotteen pysyvästi tietokannasta.",
                    "Vahvista tuotteen poisto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Kutsutaan tietokantafunktiota
                        Tietokanta.PoistaTuote(id);

                        MessageBox.Show("Tuote poistettu onnistuneesti.", "Poisto suoritettu", MessageBoxButton.OK, MessageBoxImage.Information);

                        DeleteID.Text = ""; // Tyhjennetään kenttä

                        // Päivitetään lista näytölle
                        LataaTuotteet();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Tuotteen poisto epäonnistui: {ex.Message}", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen tuotteen ID-numero poistaaksesi rivin.", "Virheellinen syöte", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}