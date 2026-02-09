using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls;
using System.Collections.ObjectModel;
using System.Windows;
using static Harjoitustyö.Uusi_Lasku;
using static Harjoitustyö.Uusi_Lasku.Lasku;
using System.Configuration;
using System;
using System.Diagnostics;

namespace Harjoitustyö
{
    public class Tietokanta
    {        
        private static string ConnectionString = ConfigurationManager.ConnectionStrings["LaskutusDB"].ConnectionString;
        private static string RootConnectionString = ConfigurationManager.ConnectionStrings["RootConnection"].ConnectionString;

        // 1. ALUSTUS: Luo käyttäjän, taulut JA lisää testidatan
        public static void AlustaTietokanta()
        {
            try
            {
                // 1. Luodaan käyttäjä (Root-tunnuksilla)
                using (MySqlConnection rootConn = new MySqlConnection(RootConnectionString))
                {
                    rootConn.Open();
                    using (MySqlCommand cmd = rootConn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE USER IF NOT EXISTS 'opiskelija'@'127.0.0.1' IDENTIFIED BY 'opiskelija1';
                            CREATE USER IF NOT EXISTS 'opiskelija'@'localhost' IDENTIFIED BY 'opiskelija1';
                            GRANT ALL PRIVILEGES ON *.* TO 'opiskelija'@'127.0.0.1';
                            GRANT ALL PRIVILEGES ON *.* TO 'opiskelija'@'localhost';
                            FLUSH PRIVILEGES;";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Luodaan tietokanta ja taulut (Opiskelija-tunnuksilla)
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = conn.CreateCommand())
                    {                      
                        // Luodaan kanta
                        cmd.CommandText = "CREATE DATABASE IF NOT EXISTS laskutusdb; USE laskutusdb;";
                        cmd.ExecuteNonQuery();

                        // Luodaan Lasku-taulu
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Lasku (
                                LaskunNumero INT AUTO_INCREMENT PRIMARY KEY,
                                Päiväys DATE,
                                Eräpäivä DATE,
                                AsiakasNimi VARCHAR(100),
                                AsiakasOsoite VARCHAR(100),
                                AsiakasPosti VARCHAR(20),
                                LaskuttajaNimi VARCHAR(100),
                                LaskuttajaOsoite VARCHAR(100),
                                LaskuttajaPosti VARCHAR(20),
                                Lisätiedot varchar(255)
                            ) AUTO_INCREMENT=100;";
                        cmd.ExecuteNonQuery();

                        // Luodaan Tuote-taulu
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Tuote (
                                tuote_id INT AUTO_INCREMENT PRIMARY KEY,
                                lasku_id INT,
                                nimi VARCHAR(100),
                                määrä INT,
                                yksikkö VARCHAR(20),
                                a_hinta DECIMAL(10, 2),
                                alv FLOAT,
                                FOREIGN KEY (lasku_id) REFERENCES Lasku(LaskunNumero)
                            );";
                        cmd.ExecuteNonQuery();

                        // 3. Lisätään testidataa
                        TestiDataGeneraattori(conn, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tietokannan alustus epäonnistui: " + ex.Message);
            }
        }

        // poistaa koko tietokannan (käytetään sovelluksen alussa, jotta saadaan puhdas kanta)
        public static void PoistaTietokanta()
        {
            // Poistetaan vanha kanta (jos on)
            try
            {
                // Käytetään Root-yhteyttä, jotta on oikeudet poistaa tietokanta
                using (MySqlConnection conn = new MySqlConnection(RootConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DROP DATABASE IF EXISTS laskutusdb;";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tietokannan poisto epäonnistui: " + ex.Message);
            }
        }


        // luo 50 testilaskua satunnaisilla tiedoilla (tiedot eivät ole loogisia, tarkoitus vain testata sovellusta)
        private static void TestiDataGeneraattori(MySqlConnection conn, MySqlCommand cmd)
        {
            Random rnd = new Random();
            // nimet
            string[] firstNames = {
            "Juhani", "Johannes", "Mikael", "Olavi", "Onni", "Matias", "Elias", "Oliver", "Ilmari", "Eemeli",
            "Maria", "Sofia", "Emilia", "Olivia", "Aino", "Amanda", "Matilda", "Helmi", "Aurora", "Ilona",
            "Johannes", "Olavi", "Eino", "Toivo", "Veikko", "Armas", "Väinö", "Ilmari", "Tauno", "Viljo",
            "Maria", "Anna", "Aino", "Aili", "Aune", "Tyyne", "Helena", "Martta", "Helmi", "Elisabet"};

            string[] lastNames = {
            "Korhonen", "Virtanen", "Mäkinen", "Nieminen", "Mäkelä",
            "Laine", "Hämäläinen", "Heikkinen", "Koskinen", "Järvinen",
            "Lehtonen", "Lehtinen", "Saarinen", "Salminen", "Heinonen",
            "Niemi", "Kinnunen", "Salo", "Turunen", "Savolainen"};

            // osoitteet
            string[] streetNames = {
            "Kauppakatu", "Mannerheimintie", "Hämeenkatu", "Kirkkokatu", "Rantatie",
            "Teollisuuskatu", "Koulukatu", "Sairaalakatu", "Satamakatu", "Puistokatu" };

            // postinumerot
            string[] cities = {
            "00100 Helsinki", "33100 Tampere", "20100 Turku", "90100 Oulu", "70100 Kuopio",
            "40100 Jyväskylä", "15100 Lahti", "28100 Pori", "65100 Vaasa", "53100 Lappeenranta" };

            string[] prducts = { "Parketin hionta ja lakkaus", "Lattialakka, matta", "Jalkalistojen asennus", "Maali, Valkoinen", "Painekyllästetty lauta", "Naulat ja kiinnikkeet",
                "Betonin valaminen", "Raudoitusverkko", "Muottien purku ja siivous", "Ikkunoiden asennus", "Ovien asennus", "Tiivisteet ja listat", "Sähköasennus", "Putkityöt",
                "Kattoremontti", "Sisustussuunnittelu", "LVI-työt", "Maalaus- ja tapetointityöt", "Kivetyksen asennus", "Terassin rakentaminen", "Väliseinien rakentaminen",
                "Lämmitysratkaisujen asennus", "Ilmanvaihtojärjestelmät", "Kylpyhuoneremontit" };

            string[] units = { "Kappale", "Kilogramma", "Neliömetri", "Metri", "Litra", "Tunti" };


            cmd.CommandText = "SELECT COUNT(*) FROM Lasku";
            long count = convertToLong(cmd.ExecuteScalar());

            if (count == 0)
            {
                using (var transaction = conn.BeginTransaction())
                {
                    cmd.Transaction = transaction;

                    for (int i = 0; i < 50; i++)
                    {
                        string rndCity = cities[rnd.Next(cities.Length)];
                        string RndStreet = streetNames[rnd.Next(streetNames.Length)];
                        int RndStreetNum = rnd.Next(1, 100);
                        string randomLast = lastNames[rnd.Next(lastNames.Length)];
                        string randomFirst = firstNames[rnd.Next(firstNames.Length)];
                        string randomName = randomFirst + " " + randomLast;
                        string street = RndStreet + " " + RndStreetNum;

                        DateTime päivämäärä = DateTime.Today.AddDays(-rnd.Next(365));
                        DateTime eräpäivä = päivämäärä.AddDays(14);
                       
                        cmd.CommandText = $@"INSERT INTO Lasku (Päiväys, Eräpäivä, AsiakasNimi, AsiakasOsoite, AsiakasPosti, LaskuttajaNimi, LaskuttajaOsoite, LaskuttajaPosti, Lisätiedot)
                                            VALUES ('{päivämäärä:yyyy-MM-dd}', '{eräpäivä:yyyy-MM-dd}', '{randomName}', '{street}', '{rndCity}', 'Rakennus OY', 'Rakennustie 15', '00100 Helsinki', '')";
                        
                        cmd.ExecuteNonQuery();
                        long lasku1_ID = cmd.LastInsertedId;

                        int numberOfProducts = rnd.Next(3, 12);

                        for (int j = 0; j < numberOfProducts; j++)
                        {
                            string randomProduct = prducts[rnd.Next(prducts.Length)];
                            int randomAmount = rnd.Next(1, 10);
                            string randomUnit = units[rnd.Next(units.Length)];

                            decimal randomPrice = Convert.ToDecimal(rnd.Next(10, 200) + rnd.NextDouble());
                            float Alv = 24.0f;

                            cmd.CommandText = $@"
                                INSERT INTO Tuote (lasku_id, nimi, määrä, yksikkö, a_hinta, alv) 
                                VALUES ({lasku1_ID}, '{randomProduct}', {randomAmount}, '{randomUnit}', 
                                {randomPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 
                                {Alv.ToString(System.Globalization.CultureInfo.InvariantCulture)});";

                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        // Apu-funktio pitkän luvun muuntamiseen turvallisesti, käytetään testidatan luomisessa
        private static long convertToLong(object o)
        {
            if (o == DBNull.Value || o == null) return 0;
            return Convert.ToInt64(o);
        }

        // hakee seuraavan laskunumeron tietokannasta, Uusi Lasku näkymässä
        public static int HaeSeuraavaLaskunNumero()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT AUTO_INCREMENT FROM information_schema.tables WHERE table_name = 'Lasku' AND table_schema = 'laskutusdb';";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Laskunumeron haku epäonnistui: " + ex.Message);
            }
            return 100000;
        }

        // tallentaa uuden laskun tietokantaan, uusi lasku näkymässä
        public static bool TallennaLasku(Lasku lasku)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    string laskuSql = @"INSERT INTO Lasku 
                                (Päiväys, Eräpäivä, AsiakasNimi, AsiakasOsoite, AsiakasPosti, LaskuttajaNimi, LaskuttajaOsoite, LaskuttajaPosti, Lisätiedot) 
                                VALUES 
                                (@pvm, @epvm, @animi, @aos, @aposti, @lnimi, @los, @lposti, @lisatiedot)";

                    MySqlCommand cmd = new MySqlCommand(laskuSql, connection);
                    cmd.Parameters.AddWithValue("@pvm", lasku.Päiväys);
                    cmd.Parameters.AddWithValue("@epvm", lasku.Eräpäivä);
                    cmd.Parameters.AddWithValue("@animi", lasku.AsiakasInfo.Nimi);
                    cmd.Parameters.AddWithValue("@aos", lasku.AsiakasInfo.Osoite);
                    cmd.Parameters.AddWithValue("@aposti", lasku.AsiakasInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lnimi", lasku.LaskuttajaInfo.Nimi);
                    cmd.Parameters.AddWithValue("@los", lasku.LaskuttajaInfo.Osoite);
                    cmd.Parameters.AddWithValue("@lposti", lasku.LaskuttajaInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lisatiedot", lasku.AsiakasInfo.Lisätiedot ?? "");

                    cmd.ExecuteNonQuery();

                    long uusiId = cmd.LastInsertedId;
                    lasku.LaskunNumero = (int)uusiId;

                    string tuoteSql = @"INSERT INTO Tuote 
                                (lasku_id, nimi, määrä, yksikkö, a_hinta, alv) 
                                VALUES 
                                (@lid, @tnimi, @tm, @tyks, @thinta, @talv)";

                    foreach (var tuote in lasku.Tuotteet)
                    {
                        MySqlCommand tuoteCmd = new MySqlCommand(tuoteSql, connection);
                        tuoteCmd.Parameters.AddWithValue("@lid", uusiId);
                        tuoteCmd.Parameters.AddWithValue("@tnimi", tuote.Nimi);
                        tuoteCmd.Parameters.AddWithValue("@tm", tuote.Määrä);
                        tuoteCmd.Parameters.AddWithValue("@tyks", tuote.Yksikkö);
                        tuoteCmd.Parameters.AddWithValue("@thinta", tuote.A_Hinta);
                        tuoteCmd.Parameters.AddWithValue("@talv", tuote.ALV);
                        tuoteCmd.ExecuteNonQuery();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tallennus epäonnistui: " + ex.Message);
                    return false;
                }
            }
        }

        // poistaa laskun id:llä, kaikki laskut näkymässä
        public static bool PoistaLasku(int id)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    // Poistetaan ensin laskuun liittyvät tuotteet
                    string deleteTuotteetSql = "DELETE FROM Tuote WHERE lasku_id = @id";
                    using (MySqlCommand cmd1 = new MySqlCommand(deleteTuotteetSql, connection))
                    {
                        cmd1.Parameters.AddWithValue("@id", id);
                        cmd1.ExecuteNonQuery();
                    }

                    // Sitten poistetaan itse lasku
                    string deleteLaskuSql = "DELETE FROM Lasku WHERE LaskunNumero = @id";
                    using (MySqlCommand cmd2 = new MySqlCommand(deleteLaskuSql, connection))
                    {
                        cmd2.Parameters.AddWithValue("@id", id);
                        int rows = cmd2.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Virhe poistettaessa laskua: " + ex.Message);
                    return false;
                }
            }
        }

        // hakee kaikki tuotteet Tuote lista näkymää varten
        public static ObservableCollection<Tuote> HaeKaikkiTuotteet()
        {
            ObservableCollection<Tuote> tuotteet = new ObservableCollection<Tuote>();

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT * FROM Tuote";
                    MySqlCommand cmd = new MySqlCommand(sql, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Tuote t = new Tuote
                            {
                                Tuote_ID = reader.GetInt32("tuote_id"),
                                Nimi = reader.GetString("nimi"),
                                Määrä = reader.GetInt32("määrä"),
                                Yksikkö = reader.GetString("yksikkö"),
                                A_Hinta = reader.GetDecimal("a_hinta"),
                                ALV = reader.GetFloat("alv")
                            };
                            tuotteet.Add(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tuotteiden haku epäonnistui: " + ex.Message);
                }
            }
            return tuotteet;
        }

        // päivittää tuotteen tiedot, Tuote lista näkymässä
        public static void PaivitaTuote(Tuote tuote)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string sql = @"UPDATE Tuote 
                           SET nimi = @nimi, 
                               määrä = @maara, 
                               yksikkö = @yksikko, 
                               a_hinta = @hinta, 
                               alv = @alv 
                           WHERE tuote_id = @id";

                    MySqlCommand cmd = new MySqlCommand(sql, connection);

                    cmd.Parameters.AddWithValue("@nimi", tuote.Nimi);
                    cmd.Parameters.AddWithValue("@maara", tuote.Määrä);
                    cmd.Parameters.AddWithValue("@yksikko", tuote.Yksikkö);
                    cmd.Parameters.AddWithValue("@hinta", tuote.A_Hinta);
                    cmd.Parameters.AddWithValue("@alv", tuote.ALV);
                    cmd.Parameters.AddWithValue("@id", tuote.Tuote_ID);

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Virhe päivityksessä: " + ex.Message);
                }
            }
        }

        // poistaa tuotteen id:llä, Tuote lista näkymässä
        public static void PoistaTuote(int id)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    string sql = @"DELETE FROM Tuote WHERE tuote_id = @id";

                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Virhe poistossa: " + ex.Message);
                }
            }
        }

        // hakee kaikki laskut Kaikki Laskut näkymää varten
        public static ObservableCollection<Lasku> HaeKaikkiLaskut()
        {
            ObservableCollection<Lasku> laskut = new ObservableCollection<Lasku>();

            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();

                    // Vaihe 1: Hae laskujen otsikkotiedot
                    string sql = "SELECT * FROM Lasku";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Lasku l = new Lasku();
                            l.LaskunNumero = reader.GetInt32("LaskunNumero");
                            l.Päiväys = reader.GetDateTime("Päiväys");
                            l.Eräpäivä = reader.GetDateTime("Eräpäivä");
                            l.AsiakasInfo.Nimi = reader.GetString("AsiakasNimi");
                            l.AsiakasInfo.Osoite = reader.GetString("AsiakasOsoite");
                            l.AsiakasInfo.Postinumero = reader.GetString("AsiakasPosti");
                            l.AsiakasInfo.Lisätiedot = reader.IsDBNull(reader.GetOrdinal("Lisätiedot")) ? "" : reader.GetString("Lisätiedot");

                            laskut.Add(l);
                        }
                    }

                    // Vaihe 2: Hae jokaiselle laskulle sen tuotteet (jotta summa lasketaan oikein)
                    foreach (var lasku in laskut)
                    {
                        lasku.Tuotteet = HaeTuotteetLaskulle(lasku.LaskunNumero);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Virhe haettaessa laskuja: " + ex.Message);
                }
            }
            return laskut;
        }

        // Apu-metodi: Hakee tuotteet tietylle lasku-ID:lle
        public static ObservableCollection<Tuote> HaeTuotteetLaskulle(int laskuID)
        {
            ObservableCollection<Tuote> tuotteet = new ObservableCollection<Tuote>();

            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Tuote WHERE lasku_id = @lid";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@lid", laskuID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Tuote t = new Tuote
                            {
                                Tuote_ID = reader.GetInt32("tuote_id"),
                                Nimi = reader.GetString("nimi"),
                                Määrä = reader.GetInt32("määrä"),
                                Yksikkö = reader.GetString("yksikkö"),
                                A_Hinta = reader.GetDecimal("a_hinta"),
                                ALV = reader.GetFloat("alv")
                            };
                            tuotteet.Add(t);
                        }
                    }
                }
            }
            return tuotteet;
        }


        // päivittää olemassa olevan laskun tiedot, Muokkaa Laskua näkymässä
        public static bool PäivitäLasku(Lasku lasku)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    // 1. PÄIVITÄ OTSIKKOTIEDOT
                    string laskuSql = @"UPDATE Lasku SET 
                                Päiväys = @pvm, 
                                Eräpäivä = @epvm, 
                                AsiakasNimi = @animi, 
                                AsiakasOsoite = @aos, 
                                AsiakasPosti = @aposti, 
                                LaskuttajaNimi = @lnimi, 
                                LaskuttajaOsoite = @los, 
                                LaskuttajaPosti = @lposti,
                                Lisätiedot = @lisatiedot
                                WHERE LaskunNumero = @lnro";

                    MySqlCommand cmd = new MySqlCommand(laskuSql, connection);
                    cmd.Parameters.AddWithValue("@pvm", lasku.Päiväys);
                    cmd.Parameters.AddWithValue("@epvm", lasku.Eräpäivä);
                    cmd.Parameters.AddWithValue("@animi", lasku.AsiakasInfo.Nimi);
                    cmd.Parameters.AddWithValue("@aos", lasku.AsiakasInfo.Osoite);
                    cmd.Parameters.AddWithValue("@aposti", lasku.AsiakasInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lnimi", lasku.LaskuttajaInfo.Nimi);
                    cmd.Parameters.AddWithValue("@los", lasku.LaskuttajaInfo.Osoite);
                    cmd.Parameters.AddWithValue("@lposti", lasku.LaskuttajaInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lisatiedot", lasku.AsiakasInfo.Lisätiedot);
                    cmd.Parameters.AddWithValue("@lnro", lasku.LaskunNumero);
                    cmd.ExecuteNonQuery();

                    // 2. POISTA VANHAT TUOTTEET
                    string deleteSql = "DELETE FROM Tuote WHERE lasku_id = @lid";
                    MySqlCommand deleteCmd = new MySqlCommand(deleteSql, connection);
                    deleteCmd.Parameters.AddWithValue("@lid", lasku.LaskunNumero);
                    deleteCmd.ExecuteNonQuery();

                    // 3. LISÄÄ NYKYISET TUOTTEET UUDELLEEN
                    string tuoteSql = @"INSERT INTO Tuote 
                                (lasku_id, nimi, määrä, yksikkö, a_hinta, alv) 
                                VALUES 
                                (@lid, @tnimi, @tm, @tyks, @thinta, @talv)";

                    foreach (var tuote in lasku.Tuotteet)
                    {
                        MySqlCommand tuoteCmd = new MySqlCommand(tuoteSql, connection);
                        tuoteCmd.Parameters.AddWithValue("@lid", lasku.LaskunNumero);
                        tuoteCmd.Parameters.AddWithValue("@tnimi", tuote.Nimi);
                        tuoteCmd.Parameters.AddWithValue("@tm", tuote.Määrä);
                        tuoteCmd.Parameters.AddWithValue("@tyks", tuote.Yksikkö);
                        tuoteCmd.Parameters.AddWithValue("@thinta", tuote.A_Hinta);
                        tuoteCmd.Parameters.AddWithValue("@talv", tuote.ALV);
                        tuoteCmd.ExecuteNonQuery();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Päivitys epäonnistui: " + ex.Message);
                    return false;
                }
            }
        }

        // HAE YKSI LASKU ID:llä, Muokkaa Laskua näkymässä
        public static Lasku HaeLasku(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM Lasku WHERE LaskunNumero = @id";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Lasku l = new Lasku();
                                l.LaskunNumero = reader.GetInt32("LaskunNumero");
                                l.Päiväys = reader.GetDateTime("Päiväys");
                                l.Eräpäivä = reader.GetDateTime("Eräpäivä");
                                l.AsiakasInfo.Nimi = reader.GetString("AsiakasNimi");
                                l.AsiakasInfo.Osoite = reader.GetString("AsiakasOsoite");
                                l.AsiakasInfo.Postinumero = reader.GetString("AsiakasPosti");
                                int ordinal = reader.GetOrdinal("Lisätiedot");
                                l.AsiakasInfo.Lisätiedot = reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
                                return l;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Virhe haettaessa laskua: " + ex.Message);
                }
            }
            return null;
        }

        // HAE YKSI LASKU NIMELLÄ, Uusi Lasku näkymässä
        public static Lasku HaeNimellä(string nimi)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM Lasku WHERE AsiakasNimi = @nimi ORDER BY Päiväys DESC LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nimi", nimi);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Lasku l = new Lasku();
                                l.LaskunNumero = reader.GetInt32("LaskunNumero");
                                l.Päiväys = reader.GetDateTime("Päiväys");
                                l.Eräpäivä = reader.GetDateTime("Eräpäivä");
                                l.AsiakasInfo.Nimi = reader.GetString("AsiakasNimi");
                                l.AsiakasInfo.Osoite = reader.GetString("AsiakasOsoite");
                                l.AsiakasInfo.Postinumero = reader.GetString("AsiakasPosti");
                                int ordinal = reader.GetOrdinal("Lisätiedot");
                                l.AsiakasInfo.Lisätiedot = reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
                                return l;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Virhe haettaessa laskua: " + ex.Message);
                }
            }
            return null;
        }
    }
}