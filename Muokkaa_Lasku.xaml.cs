using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using static Harjoitustyö.Barcode;
using static Harjoitustyö.PdfService;
using static Harjoitustyö.Tietokanta;
using static Harjoitustyö.Uusi_Lasku;

namespace Harjoitustyö
{

    public partial class Muokkaa_Lasku : Window
    {
        public Muokkaa_Lasku()
        {
            
            // Alustetaan tuotelista, jos se ei ole vielä alustettu. Tämä varmistaa, että laskun muokkausikkunalla on aina käytettävissä päivitetty tuotelista.
            if (Uusi_Lasku.VarastoTuotteet == null || Uusi_Lasku.VarastoTuotteet.Count == 0)
            {
                Uusi_Lasku.VarastoTuotteet = Tietokanta.HaeKaikkiTuotteet();
            }           
            InitializeComponent();
        }

        // Käytetään luokassa listaa, johon tallennetaan kaikki nimellä löytyneet laskut, jotta voidaan toteuttaa edellinen/seuraava -nappien toiminnallisuus.
        private System.Collections.Generic.List<Lasku> _loytyneetLaskut;
        private int _nykyinenIndeksi = 0;

        // peruutus painikkeen toteutus, joka avaa päävalikon ikkunan ja sulkee nykyisen ikkunan ilman, että muutokset tallennetaan.
        private void btnPeruuta_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko myWindow = new Päävalikko();
            myWindow.WindowState = this.WindowState;
            myWindow.Show();
            this.Close();
        }

        // Tallennus painikkeen toteutus, joka päivittää laskun tietokantaan. Ensin tarkistetaan, että DataContext on Lasku-tyyppinen olio, ja sitten kutsutaan Tietokanta-luokan PaivitaLasku-metodia.
        private void btnTallenna_Click(object sender, RoutedEventArgs e)
        {
            
            if (this.DataContext is Lasku muokattuLasku)
            {
                if (!OnkoTiedotKelvolliset(muokattuLasku)) return;
                if (muokattuLasku.Eräpäivä < muokattuLasku.Päiväys)
                {
                    MessageBox.Show("Eräpäivä ei voi olla aiemmin kuin laskun päiväys!", "Virheellinen päivämäärä", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // 2. Call the database update method
                bool onnistui = Tietokanta.PaivitaLasku(muokattuLasku);

                if (onnistui)
                {
                    MessageBox.Show("Muutokset tallennettu onnistuneesti!");
                }
            }
            else
            {
                MessageBox.Show("Ei tallennettavia tietoja. Hae ensin lasku.");
            }
        }

        // Toteutetaan haku laskun numerolla -painikkeelle, joka hakee laskun sen numeron perusteella.
        private void btnHae_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtLaskunNumero.Text, out int id))
            {
                // Haetaan laskun perustiedot tietokannasta
                Lasku haettuLasku = Tietokanta.HaeLasku(id);

                if (haettuLasku != null)
                {
                    // Haetaan laskulle kuuluvat tuotteet
                    haettuLasku.Tuotteet = Tietokanta.HaeTuotteetLaskulle(id);

                    // Asetetaan ikkunan DataContext löydetyksi laskuksi.
                    BarcodeImage.Source = Barcode.GenerateBarcode(haettuLasku.LaskunNumero.ToString());
                    this.DataContext = haettuLasku;
                    PäivitäSumma();
                }
                else
                {
                    MessageBox.Show("Laskua ei löytynyt numerolla " + id);
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen laskun numero.");
            }
        }

        // Päivitetään ikkunan summa, jos lasku ja tuotteet on haettu onnistuneesti.
        public void PäivitäSumma()
        {
            if (this.DataContext is Lasku nykyinenLasku && nykyinenLasku.Tuotteet != null)
            {           
                decimal summa = nykyinenLasku.Tuotteet.Sum(t => (t.Yhteensä));
                Total.Text = $"{summa:C2}";
            }
            else
            {
                Total.Text = "0,00 €";
            }
        }

        // Toteutetaan haku nimellä -painikkeelle, joka hakee laskun asiakkaan nimen perusteella. 
        private void btnHaeNimella_Click(object sender, RoutedEventArgs e)
        {
            string nimi = txtAsiakasNimi.Text;

            if (!string.IsNullOrWhiteSpace(nimi))
            {
                var tulokset = Tietokanta.HaeNimella(nimi);

                // TALLENNETAAN TULOKSET LISTAAN
                _loytyneetLaskut = new System.Collections.Generic.List<Lasku>(tulokset);

                if (_loytyneetLaskut != null && _loytyneetLaskut.Count > 0)
                {
                    // Näytetään ensimmäinen
                    PäivitäNäyttö(0);
                }
                else
                {
                    MessageBox.Show("Laskua ei löytynyt nimellä: " + nimi);
                    if (txtNavigointiInfo != null) txtNavigointiInfo.Text = "0 / 0";
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen nimi");
            }
        }

        private void BtnEdellinen_Click(object sender, RoutedEventArgs e)
        {
            // Tarkistetaan, onko lista olemassa ja voidaanko mennä taaksepäin
            if (_loytyneetLaskut != null && _nykyinenIndeksi > 0)
            {
                PäivitäNäyttö(_nykyinenIndeksi - 1);
            }
        }

        private void BtnSeuraava_Click(object sender, RoutedEventArgs e)
        {
            // Tarkistetaan, onko lista olemassa ja voidaanko mennä eteenpäin
            if (_loytyneetLaskut != null && _nykyinenIndeksi < _loytyneetLaskut.Count - 1)
            {
                PäivitäNäyttö(_nykyinenIndeksi + 1);
            }
        }

        private void PäivitäNäyttö(int uusiIndeksi)
        {
            _nykyinenIndeksi = uusiIndeksi;
            Lasku valittu = _loytyneetLaskut[_nykyinenIndeksi];

            // Haetaan tuotteet, jotta summa lasketaan oikein
            valittu.Tuotteet = Tietokanta.HaeTuotteetLaskulle(valittu.LaskunNumero);

            this.DataContext = valittu;
            if (BarcodeImage != null)
            {
                BarcodeImage.Source = Barcode.GenerateBarcode(valittu.LaskunNumero.ToString());
            }
            PäivitäSumma();

            // Päivitetään infoteksti (esim. "1 / 3")
            if (txtNavigointiInfo != null)
            {
                txtNavigointiInfo.Text = $"{_nykyinenIndeksi + 1} / {_loytyneetLaskut.Count}";
            }
        }

        private bool OnkoTiedotKelvolliset(Lasku lasku)
        {
            // 1. Päivämäärät
            if (lasku.Eräpäivä < lasku.Päiväys)
            {
                MessageBox.Show("Eräpäivä ei voi olla aiemmin kuin laskun päiväys!", "Virheellinen päivämäärä", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 2. Asiakastiedot
            if (string.IsNullOrWhiteSpace(lasku.AsiakasInfo.Nimi) ||
                string.IsNullOrWhiteSpace(lasku.AsiakasInfo.Osoite) ||
                string.IsNullOrWhiteSpace(lasku.AsiakasInfo.Postinumero))
            {
                MessageBox.Show("Täytä asiakastiedot (Nimi, Osoite, Postinumero)!", "Puuttuvat tiedot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 3. Onko rivejä?
            if (lasku.Tuotteet.Count == 0)
            {
                MessageBox.Show("Laskulla on oltava vähintään yksi tuoterivi.", "Huomio");
                return false;
            }

            // 4. Rivien sisältö (Nimi, Määrä, Hinta, Yksikkö)
            foreach (var rivi in lasku.Tuotteet)
            {
                if (string.IsNullOrEmpty(rivi.Nimi) || rivi.Määrä <= 0 || rivi.A_Hinta <= 0 || string.IsNullOrEmpty(rivi.Yksikkö))
                {
                    MessageBox.Show("Tarkista tuoterivit: Nimi, yksikkö tai hinta puuttuu, tai tiedot ovat virheelliset.", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }
    }
}
