using System;
using System.Collections.ObjectModel;
using System.Windows;
using static Harjoitustyö.Uusi_Lasku;

namespace Harjoitustyö
{
    public partial class Tuote_Lista : Window
    {
        public Tuote_Lista()
        {
            InitializeComponent();
            LataaTuotteet();
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
            // Huom: varmista että käytät oikeaa tyyppiä (Tuote eikä Lasku.Tuote)
            var tuotteet = TuoteLista.ItemsSource as ObservableCollection<Tuote>;

            if (tuotteet != null)
            {

                foreach (var tuote in tuotteet)
                {
                    // Varmistetaan, ettei tallenneta tyhjiä rivejä vahingossa
                    if (string.IsNullOrWhiteSpace(tuote.Nimi) || string.IsNullOrWhiteSpace(tuote.Yksikkö) || tuote.A_Hinta <= 0)
                    {
                        MessageBox.Show("Tuotteella täytyy olla Nimi, Yksikkö ja Hinta", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // Lopetetaan tallennus, jos löytyy tyhjä nimi
                    }
                }

                int lisatyt = 0;
                int paivitetyt = 0;

                

                foreach (var tuote in tuotteet)
                {
                    // Jos Tuote_ID on 0, se on uusi rivi -> LISÄTÄÄN
                    if (tuote.Tuote_ID == 0)
                    {
                        // Varmistetaan, ettei tallenneta tyhjiä rivejä vahingossa
                        if (!string.IsNullOrWhiteSpace(tuote.Nimi))
                        {
                            Tietokanta.LisaaTuote(tuote);
                            lisatyt++;
                        }
                    }
                    // Jos Tuote_ID > 0, se on olemassa oleva rivi -> PÄIVITETÄÄN
                    else
                    {
                        Tietokanta.PaivitaTuote(tuote);
                    }
                }

                MessageBox.Show($"Tallennettu!\nUusia tuotteita: {lisatyt}");

                // Ladataan lista uudelleen, jotta uudet tuotteet saavat ID:t tietokannasta näkyviin
                LataaTuotteet();
            }
        }

        private void PoistaRivi_Click(object sender, RoutedEventArgs e)
        {
            // 1. Haetaan painike, jota klikattiin
            if (sender is System.Windows.Controls.Button button && button.DataContext is Tuote valittuTuote)
            {
                // Tässä meillä on nyt se tuote (valittuTuote), jonka riviä painettiin.
                // Otetaan ID talteen
                int id = valittuTuote.Tuote_ID;

                // 2. Kysytään varmistus
                MessageBoxResult result = MessageBox.Show(
                    $"Haluatko varmasti poistaa tuoterivin ID: {id}?\n\nTämä poistaa tuotteen pysyvästi tietokannasta.",
                    "Vahvista tuotteen poisto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 3. Suoritetaan poisto
                        Tietokanta.PoistaTuote(id);

                        MessageBox.Show("Tuote poistettu onnistuneesti.", "Poisto suoritettu", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 4. Päivitetään lista
                        LataaTuotteet();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Tuotteen poisto epäonnistui: {ex.Message}", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }      
    }
}