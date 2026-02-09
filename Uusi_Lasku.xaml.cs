using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static Harjoitustyö.PdfService;
using static Harjoitustyö.Barcode;

namespace Harjoitustyö
{
    public partial class Uusi_Lasku : Window
    {
        public Uusi_Lasku()
        {
            InitializeComponent();

            // 1. Alustetaan tietokanta ja haetaan seuraava laskun numero
            int seuraavaNumero = Tietokanta.HaeSeuraavaLaskunNumero();
            Newlasku.LaskunNumero = seuraavaNumero;

            // 2. Luo viivakoodi näytölle
            BarcodeImage.Source = Barcode.GenerateBarcode(seuraavaNumero.ToString());

            // 3. Asetetaan DataContext
            this.DataContext = Newlasku;
        }

        // Uusi Lasku -olio, joka sitoo ikkunan kentät
        public Lasku Newlasku { get; set; } = new Lasku();

        // Tallenna-painikkeen käsittelijä
        private void Tallenna_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            // Kutsutaan uutta yhteistä luokkaa
            bool onnistui = Tietokanta.TallennaLasku(Newlasku);

            if (onnistui)
            {
                MessageBox.Show("Lasku tallennettu onnistuneesti!");

                Päävalikko MyWindow = new Päävalikko();
                MyWindow.Show();
                this.Close();
            }
        }

        // Tapahtumankäsittelijä joka luo PDF:n, avaa sen ja tallentaa tiedot tietokantaan
        private void ToPDF_AND_Save_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            try
            {               
                string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
                string folderPath = System.IO.Path.Combine(projectRoot, "Laskut");

                // 1. LUO KANSION JOS SITÄ EI OLE:
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                // 2. Rakentaa tiedostonimen ja polun
                string fileName = $"Lasku_{Newlasku.LaskunNumero}.pdf";
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                // 3. LUO PDF JA AVAA SE
                LuoPDF(Newlasku, fullPath);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });

                // 4. TALLENNA TIETOKANTAAN
                bool onnistui = Tietokanta.TallennaLasku(Newlasku);

                if (onnistui)
                {
                    MessageBox.Show("PDF luotu ja tiedot tallennettu tietokantaan.");

                    Päävalikko MyWindow = new Päävalikko();
                    MyWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe: {ex.Message}");
            }
        }

        // Metodi joka tarkistaa että kaikki tarvittavat tiedot on syötetty ennen tallennusta tai PDF:n luontia
        private bool OnkoTiedotKelvolliset()
        {
            // 1. Check if Customer info is valid
            if (string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Nimi) ||
                string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Osoite) || 
                string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Postinumero))
            {
                MessageBox.Show("Täytä asiakastiedot!", "Huomio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 2. Check if there are products
            if (Newlasku.Tuotteet.Count == 0)
            {
                MessageBox.Show("Laskulla on oltava vähintään yksi tuoterivi.", "Huomio");
                return false;
            }

           
            foreach (var tuote in Newlasku.Tuotteet)
            {
                if (!string.IsNullOrEmpty(tuote["Nimi"]) ||
                    !string.IsNullOrEmpty(tuote["Määrä"]) ||
                    !string.IsNullOrEmpty(tuote["A_Hinta"]) ||
                    !string.IsNullOrEmpty(tuote["Yksikkö"]))
                {
                    MessageBox.Show("Jokaisella tuoterivillä on oltava nimi, määrä, Yksikkö ja hinta!", "Puutteelliset tiedot", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        // Peruuta-painikkeen käsittelijä
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko MyWindow = new Päävalikko();
            MyWindow.Show();
            this.Close();
        }

        // PäivitäSumma-metodi joka päivittää kokonaissumman näytölle
        public void PäivitäSumma()
        {
            if (Newlasku != null && Newlasku.Tuotteet != null)
            {
                // LINQ Sum ensures we calculate based on current items
                decimal summa = Newlasku.Tuotteet.Sum(t => t.Yhteensä);
                Total.Text = $"{summa:C2}";
            }
        }

        // Tapahtumankäsittelijä joka reagoi rivin muokkauksen päättymiseen DataGridissä
        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => PäivitäSumma()));
        }

        // Lasku-luokka ja sen sisäluokat
        public class Lasku : INotifyPropertyChanged
        {
            public DateTime Päiväys { get; set; } = DateTime.Now;
            public DateTime Eräpäivä { get; set; } = DateTime.Now.AddDays(28);
            private int _laskunNumero;
            public int LaskunNumero
            {
                get { return _laskunNumero; }
                set
                {
                    _laskunNumero = value;
                    OnPropertyChanged("LaskunNumero");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            // Tuotelista
            public ObservableCollection<Tuote> Tuotteet { get; set; } = new ObservableCollection<Tuote>();

            // LaskuttajaInfo ja AsiakasInfo ominaisuudet
            public Laskuttaja LaskuttajaInfo { get; set; }
            public Asiakas AsiakasInfo { get; set; }

            public Lasku()
            {
                LaskuttajaInfo = new Laskuttaja();
                AsiakasInfo = new Asiakas();
            }

            // Laskuttaja sisäluokka ja sen ominaisuudet
            public class Laskuttaja
            {
                public string Nimi { get; set; } = "Rakennus OY";
                public string Osoite { get; set; } = "Rakennustie 15";
                public string Postinumero { get; set; } = "00100 Helsinki";
            }

            // Asiakas sisäluokka ja sen ominaisuudet
            public class Asiakas : IDataErrorInfo
            {
                public string Nimi { get; set; }
                public string Osoite { get; set; }
                public string Postinumero { get; set; }
                public string Lisätiedot { get; set; } = "";

                public string this[string columnName]
                {
                    get
                    {
                        if (columnName == "Nimi" && string.IsNullOrWhiteSpace(Nimi))
                            return "Asiakkaan nimi on pakollinen.";
                        if (columnName == "Osoite" && string.IsNullOrWhiteSpace(Osoite))
                            return "Osoite on pakollinen.";
                        return null;
                    }
                }
                public string Error => null;
            }

            // Tuote sisäluokka ja sen ominaisuudet
            public class Tuote : INotifyPropertyChanged, IDataErrorInfo
            {
                public int Tuote_ID { get; set; }
                private string _nimi;
                private int _määrä;
                private string _yksikkö;
                private decimal _a_hinta;
                private float _alv = 24;

                public string this[string columnName]
                {
                    get
                    {
                        if (columnName == "Nimi" && string.IsNullOrWhiteSpace(Nimi))
                            return "Tuotteen nimi puuttuu.";
                        if (columnName == "Määrä" && Määrä <= 0)
                            return "Määrän oltava yli 0.";
                        if (columnName == "Yksikkö" && string.IsNullOrWhiteSpace(Yksikkö))
                            return "Valitse yksikkö.";
                        if (columnName == "A_Hinta" && A_Hinta <= 0)
                            return "Hinta ei voi olla 0.";
                        return null;
                    }
                }
                public string Error => null;

                public decimal ALV_Euro
                {
                    get
                    {
                        return (A_Hinta * ((decimal)ALV / 100m)) * Määrä;
                    }
                }

                public string Nimi
                {
                    get { return _nimi; }
                    set { _nimi = value; OnPropertyChanged("Nimi"); }
                }

                public int Määrä
                {
                    get { return _määrä; }
                    set
                    {
                        _määrä = value;
                        OnPropertyChanged("Määrä");
                        OnPropertyChanged("ALV_Euro");
                        OnPropertyChanged("Yhteensä");
                    }
                }

                public string Yksikkö
                {
                    get { return _yksikkö; }
                    set { _yksikkö = value; OnPropertyChanged("Yksikkö"); }
                }

                public decimal A_Hinta
                {
                    get { return _a_hinta; }
                    set
                    {
                        _a_hinta = value;
                        OnPropertyChanged("A_Hinta");
                        OnPropertyChanged("ALV_Euro");
                        OnPropertyChanged("Yhteensä");
                    }
                }

                public float ALV
                {
                    get { return _alv; }
                    set { _alv = value; OnPropertyChanged("ALV"); }
                }

                public decimal Yhteensä
                {
                    get
                    {
                        return (Määrä * A_Hinta) + ALV_Euro;
                    }
                }

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged(string name)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }
            }

            public decimal ALV_Euro_Yhteensä
            {
                get
                {
                    decimal summa = 0;
                    foreach (var tuote in Tuotteet)
                    {
                        summa += tuote.ALV_Euro;
                    }
                    return summa;
                }
            }

            // Metodi joka laskee laskun kokonaissumma
            public decimal Yhteensä
            {
                get
                {
                    decimal summa = 0;
                    foreach (var tuote in Tuotteet)
                    {
                        summa += tuote.Yhteensä;
                    }
                    return summa;
                }
            }

            public string PDFPolku
            {
                get
                {
                    return $"Lataa Lasku_{LaskunNumero}.pdf";
                }
            }
        }
    }
}