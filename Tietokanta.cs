using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
// Tärkeä: Käytetään Harjoitustyö-nimiavaruutta, jossa Models.cs luokat ovat
using Harjoitustyö;

namespace Harjoitustyö
{
    public class Tietokanta
    {
        private static string ConnectionString = ConfigurationManager.ConnectionStrings["LaskutusDB"].ConnectionString;
        private static string RootConnectionString = ConfigurationManager.ConnectionStrings["RootConnection"].ConnectionString;

        // --- TIETOKANNAN HALLINTA ---

        public static void PoistaTietokanta()
        {
            try
            {
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

        public static void AlustaTietokanta()
        {
            try
            {
                using (MySqlConnection rootConn = new MySqlConnection(RootConnectionString))
                {
                    rootConn.Open();
                    using (MySqlCommand cmd = rootConn.CreateCommand())
                    {
                        // 1. Käyttäjät
                        cmd.CommandText = @"
                            CREATE USER IF NOT EXISTS 'opiskelija'@'127.0.0.1' IDENTIFIED BY 'opiskelija1';
                            CREATE USER IF NOT EXISTS 'opiskelija'@'localhost' IDENTIFIED BY 'opiskelija1';
                            GRANT ALL PRIVILEGES ON *.* TO 'opiskelija'@'127.0.0.1';
                            GRANT ALL PRIVILEGES ON *.* TO 'opiskelija'@'localhost';
                            FLUSH PRIVILEGES;";
                        cmd.ExecuteNonQuery();

                        // 2. Kanta
                        cmd.CommandText = "CREATE DATABASE IF NOT EXISTS laskutusdb CHARACTER SET utf8mb4 COLLATE utf8mb4_swedish_ci; USE laskutusdb;";
                        cmd.ExecuteNonQuery();

                        // 3. Taulut
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

                        // Tuote (Varasto)
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Tuote (
                                tuote_id INT AUTO_INCREMENT PRIMARY KEY,
                                nimi VARCHAR(100),
                                määrä INT DEFAULT 0,
                                yksikkö VARCHAR(20),
                                a_hinta DECIMAL(10, 2),
                                alv DECIMAL(5, 2)
                            );";
                        cmd.ExecuteNonQuery();

                        // Laskurivi (Linkki laskun ja tuotteen välillä)
                        // ON DELETE SET NULL: Jos tuote poistetaan rekisteristä, laskurivi säilyy mutta linkki katkeaa.
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Laskurivi (
                                laskurivi_id INT AUTO_INCREMENT PRIMARY KEY,
                                lasku_id INT,
                                tuote_id INT,
                                nimi VARCHAR(100),
                                määrä INT,
                                yksikkö VARCHAR(20),
                                a_hinta DECIMAL(10, 2),
                                alv DECIMAL(5, 2),
                                FOREIGN KEY (lasku_id) REFERENCES Lasku(LaskunNumero) ON DELETE CASCADE,
                                FOREIGN KEY (tuote_id) REFERENCES Tuote(tuote_id) ON DELETE SET NULL
                            );";
                        cmd.ExecuteNonQuery();

                        // 4. Testidata
                        TestiDataGeneraattori(rootConn, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tietokannan alustus epäonnistui: " + ex.Message);
            }
        }

        private static void TestiDataGeneraattori(MySqlConnection conn, MySqlCommand cmd)
        {
            // Määritellään, kuinka monta laskua luodaan
            int laskujenMaara = 50;

            cmd.CommandText = "USE laskutusdb;";
            cmd.ExecuteNonQuery();

            Random rnd = new Random();

            cmd.CommandText = "SELECT COUNT(*) FROM Lasku";
            long count = Convert.ToInt64(cmd.ExecuteScalar());

            // Ajetaan vain, jos tietokanta on tyhjä
            if (count == 0)
            {
                using (var transaction = conn.BeginTransaction())
                {
                    cmd.Transaction = transaction;

                    // --- 1. Luodaan tuotteet ---
                    string[] prducts = { "Parketin hionta", "Maalaus", "Laminaatti", "Listat", "Tasoite", "Ruuvit", "Sähkötyöt", "Putkityöt", "Siivous", "Kuljetus" };
                    string[] units = { "kpl", "m2", "pkt", "m", "kg", "h" };
                    List<int> createdProductIds = new List<int>();

                    foreach (string prodName in prducts)
                    {
                        string randomUnit = units[rnd.Next(units.Length)];
                        decimal randomPrice = Convert.ToDecimal(rnd.Next(10, 200));

                        //Käytetään parametreja tai InvariantCulture hinnassa SQL-virheiden välttämiseksi
                        cmd.CommandText = $"INSERT INTO Tuote (nimi, yksikkö, a_hinta, alv, määrä) VALUES ('{prodName}', '{randomUnit}', " +
                            $"{randomPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 25.5, 100)";
                        cmd.ExecuteNonQuery();
                        createdProductIds.Add((int)cmd.LastInsertedId);
                    }
                 
                    // Nimilistat satunnaistusta varten
                    string[] etunimet = { "Matti", "Pekka", "Liisa", "Maija", "Kalle", "Ville", "Anna", "Eeva", "Jari", "Sari", "Antti", "Minna", "Timo", "Kirsi" };
                    string[] sukunimet = { "Korhonen", "Virtanen", "Mäkinen", "Nieminen", "Mäkelä", "Hämäläinen", "Laine", "Heikkinen", "Koskinen", "Järvinen", "Lehtonen", "Leinonen" };

                    // Lisätään myös muutama yritys vaihtelun vuoksi
                    string[] yritykset = { "Rakennus Oy", "Putki & Sähkö Ky", "Kuljetusliike Matkahuolto", "Tmi Siivouspalvelu", "Kiinteistöhuolto K.K.", "Maalausliike Väri" };

                    for (int i = 0; i < laskujenMaara; i++)
                    {
                        string cName;

                        // Arvotaan: 20% todennäköisyydellä yritys, 80% todennäköisyydellä henkilö
                        if (rnd.Next(0, 10) < 2)
                        {
                            cName = yritykset[rnd.Next(yritykset.Length)];
                        }
                        else
                        {
                            string etunimi = etunimet[rnd.Next(etunimet.Length)];
                            string sukunimi = sukunimet[rnd.Next(sukunimet.Length)];
                            cName = $"{etunimi} {sukunimi}";
                        }

                        // Luodaan lasku tälle asiakkaalle
                        cmd.CommandText = $"INSERT INTO Lasku (Päiväys, Eräpäivä, AsiakasNimi, AsiakasOsoite, AsiakasPosti, LaskuttajaNimi, LaskuttajaOsoite, LaskuttajaPosti, Lisätiedot)" +
                            $" VALUES (CURDATE(), CURDATE(), '{cName}', 'Testitie 1', '00100', 'Rakennus Oy', 'Tie 15', '00100', '')";
                        cmd.ExecuteNonQuery();
                        long laskuId = cmd.LastInsertedId;

                        // --- 3. Luodaan laskurivit ---
                        int riviMaara = rnd.Next(2, 6);
                        for (int j = 0; j < riviMaara; j++)
                        {
                            int prodId = createdProductIds[rnd.Next(createdProductIds.Count)];
                            int qty = rnd.Next(1, 10);

                            cmd.CommandText = $"INSERT INTO Laskurivi (lasku_id, tuote_id, nimi, määrä, yksikkö, a_hinta, alv) SELECT {laskuId}, tuote_id, nimi, {qty}, yksikkö, a_hinta, alv FROM Tuote WHERE tuote_id = {prodId}";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public static int HaeSeuraavaLaskunNumero()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                {
                    conn.Open();
                    object result = new MySqlCommand("SELECT AUTO_INCREMENT FROM information_schema.tables WHERE table_name = 'Lasku' AND table_schema = 'laskutusdb';", conn).ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 100000;
                }
            }
            catch { return 100000; }
        }

        // --- LASKUJEN KÄSITTELY ---

        public static bool TallennaLasku(Lasku lasku)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();

                    // 1. Laskun otsikko
                    string sql = @"INSERT INTO Lasku (Päiväys, Eräpäivä, AsiakasNimi, AsiakasOsoite, AsiakasPosti, LaskuttajaNimi, LaskuttajaOsoite, LaskuttajaPosti, Lisätiedot) 
                           VALUES (@p, @ep, @an, @ao, @ap, @ln, @lo, @lp, @lisa)";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p", lasku.Päiväys);
                    cmd.Parameters.AddWithValue("@ep", lasku.Eräpäivä);
                    cmd.Parameters.AddWithValue("@an", lasku.AsiakasInfo.Nimi);
                    cmd.Parameters.AddWithValue("@ao", lasku.AsiakasInfo.Osoite);
                    cmd.Parameters.AddWithValue("@ap", lasku.AsiakasInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@ln", lasku.LaskuttajaInfo.Nimi);
                    cmd.Parameters.AddWithValue("@lo", lasku.LaskuttajaInfo.Osoite);
                    cmd.Parameters.AddWithValue("@lp", lasku.LaskuttajaInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lisa", lasku.AsiakasInfo.Lisätiedot ?? "");
                    cmd.ExecuteNonQuery();

                    lasku.LaskunNumero = (int)cmd.LastInsertedId;

                    // 2. Laskurivit
                    string rowSql = @"INSERT INTO Laskurivi (lasku_id, tuote_id, nimi, määrä, yksikkö, a_hinta, alv) 
                              VALUES (@lid, @tid, @n, @m, @y, @h, @a)";

                    foreach (var rivi in lasku.Tuotteet)
                    {
                        // Jos Tuote_ID on 0 (eli käyttäjä kirjoitti nimen käsin), tallennetaan se ensin Tuote-rekisteriin
                        if (rivi.Tuote_ID == 0 && !string.IsNullOrWhiteSpace(rivi.Nimi))
                        {
                            string insertTuoteSql = "INSERT INTO Tuote (nimi, määrä, yksikkö, a_hinta, alv) VALUES (@tn, 0, @ty, @th, @ta)";
                            using (MySqlCommand tCmd = new MySqlCommand(insertTuoteSql, conn))
                            {
                                tCmd.Parameters.AddWithValue("@tn", rivi.Nimi);
                                tCmd.Parameters.AddWithValue("@ty", rivi.Yksikkö);
                                tCmd.Parameters.AddWithValue("@th", rivi.A_Hinta);
                                tCmd.Parameters.AddWithValue("@ta", rivi.ALV);
                                tCmd.ExecuteNonQuery();

                                // Päivitetään rivin Tuote_ID vastaamaan uutta tietokantariiviä
                                rivi.Tuote_ID = (int)tCmd.LastInsertedId;
                            }
                        }
                        // --- UUSI LOGIIKKA PÄÄTTYY ---

                        MySqlCommand rCmd = new MySqlCommand(rowSql, conn);
                        rCmd.Parameters.AddWithValue("@lid", lasku.LaskunNumero);
                        rCmd.Parameters.AddWithValue("@tid", rivi.Tuote_ID == 0 ? (object)DBNull.Value : rivi.Tuote_ID);
                        rCmd.Parameters.AddWithValue("@n", rivi.Nimi);
                        rCmd.Parameters.AddWithValue("@m", rivi.Määrä);
                        rCmd.Parameters.AddWithValue("@y", rivi.Yksikkö);
                        rCmd.Parameters.AddWithValue("@h", rivi.A_Hinta);
                        rCmd.Parameters.AddWithValue("@a", rivi.ALV);
                        rCmd.ExecuteNonQuery();
                    }

                    // Päivitetään staattinen tuotelista, jotta uusi tuote näkyy heti valikoissa
                    if (Uusi_Lasku.VarastoTuotteet != null)
                    {
                        // Tämän voi tehdä myös hakemalla kannasta uudestaan, mutta tämä on nopeampi:
                        // Uusi_Lasku.VarastoTuotteet = HaeKaikkiTuotteet(); 
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tallennusvirhe: " + ex.Message);
                    return false;
                }
            }
        }

        public static ObservableCollection<Lasku> HaeKaikkiLaskut()
        {
            var lista = new ObservableCollection<Lasku>();
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlDataReader r = new MySqlCommand("SELECT * FROM Lasku", conn).ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var l = new Lasku
                            {
                                LaskunNumero = r.GetInt32("LaskunNumero"),
                                Päiväys = r.GetDateTime("Päiväys"),
                                Eräpäivä = r.GetDateTime("Eräpäivä"),
                            };
                            l.AsiakasInfo.Nimi = r.GetString("AsiakasNimi");
                            l.AsiakasInfo.Osoite = r.GetString("AsiakasOsoite");
                            l.AsiakasInfo.Postinumero = r.GetString("AsiakasPosti");
                            l.AsiakasInfo.Lisätiedot = r.IsDBNull(r.GetOrdinal("Lisätiedot")) ? "" : r.GetString("Lisätiedot");

                            lista.Add(l);
                        }
                    }

                    // Haetaan rivit jokaiselle laskulle
                    foreach (var lasku in lista)
                    {
                        lasku.Tuotteet = HaeTuotteetLaskulle(lasku.LaskunNumero);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Haku virhe: " + ex.Message); }
            }
            return lista;
        }

        public static ObservableCollection<Laskurivi> HaeTuotteetLaskulle(int laskuID)
        {
            var rivit = new ObservableCollection<Laskurivi>();
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM Laskurivi WHERE lasku_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", laskuID);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            rivit.Add(new Laskurivi
                            {
                                Laskurivi_ID = r.GetInt32("laskurivi_id"),
                                Tuote_ID = r.IsDBNull(r.GetOrdinal("tuote_id")) ? 0 : r.GetInt32("tuote_id"),
                                Nimi = r.GetString("nimi"),
                                Määrä = r.GetInt32("määrä"),
                                Yksikkö = r.GetString("yksikkö"),
                                A_Hinta = r.GetDecimal("a_hinta"),
                                ALV = r.GetDecimal("alv")
                            });
                        }
                    }
                }
            }
            return rivit;
        }

        public static bool PoistaLasku(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Foreign Key CASCADE hoitaa rivien poiston, mutta varmistetaan
                    new MySqlCommand($"DELETE FROM Laskurivi WHERE lasku_id={id}", conn).ExecuteNonQuery();
                    new MySqlCommand($"DELETE FROM Lasku WHERE LaskunNumero={id}", conn).ExecuteNonQuery();
                    return true;
                }
                catch { return false; }
            }
        }

        // --- TUOTTEIDEN KÄSITTELY (VARASTO) ---

        public static ObservableCollection<Tuote> HaeKaikkiTuotteet()
        {
            var tuotteet = new ObservableCollection<Tuote>();
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlDataReader r = new MySqlCommand("SELECT * FROM Tuote", conn).ExecuteReader())
                    {
                        while (r.Read())
                        {
                            tuotteet.Add(new Tuote
                            {
                                Tuote_ID = r.GetInt32("tuote_id"),
                                Nimi = r.GetString("nimi"),
                                Määrä = r.GetInt32("määrä"),
                                Yksikkö = r.GetString("yksikkö"),
                                A_Hinta = r.GetDecimal("a_hinta"),
                                ALV = r.GetDecimal("alv")
                            });
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Tuotehaku virhe: " + ex.Message); }
            }
            return tuotteet;
        }

        public static void LisaaTuote(Tuote tuote)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("INSERT INTO Tuote (nimi, määrä, yksikkö, a_hinta, alv) VALUES (@n, @m, @y, @h, @a)", conn);
                cmd.Parameters.AddWithValue("@n", tuote.Nimi);
                cmd.Parameters.AddWithValue("@m", tuote.Määrä);
                cmd.Parameters.AddWithValue("@y", tuote.Yksikkö);
                cmd.Parameters.AddWithValue("@h", tuote.A_Hinta);
                cmd.Parameters.AddWithValue("@a", tuote.ALV);
                cmd.ExecuteNonQuery();
            }
        }

        public static void PaivitaTuote(Tuote tuote)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE Tuote SET nimi=@n, määrä=@m, yksikkö=@y, a_hinta=@h, alv=@a WHERE tuote_id=@id", conn);
                cmd.Parameters.AddWithValue("@n", tuote.Nimi);
                cmd.Parameters.AddWithValue("@m", tuote.Määrä);
                cmd.Parameters.AddWithValue("@y", tuote.Yksikkö);
                cmd.Parameters.AddWithValue("@h", tuote.A_Hinta);
                cmd.Parameters.AddWithValue("@a", tuote.ALV);
                cmd.Parameters.AddWithValue("@id", tuote.Tuote_ID);
                cmd.ExecuteNonQuery();
            }
        }
        public static void PoistaTuote(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                new MySqlCommand($"DELETE FROM Tuote WHERE tuote_id={id}", conn).ExecuteNonQuery();
            }
        }
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
                        using (MySqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                var l = new Lasku
                                {
                                    LaskunNumero = r.GetInt32("LaskunNumero"),
                                    Päiväys = r.GetDateTime("Päiväys"),
                                    Eräpäivä = r.GetDateTime("Eräpäivä"),
                                };
                                l.AsiakasInfo.Nimi = r.GetString("AsiakasNimi");
                                l.AsiakasInfo.Osoite = r.GetString("AsiakasOsoite");
                                l.AsiakasInfo.Postinumero = r.GetString("AsiakasPosti");
                                l.AsiakasInfo.Lisätiedot = r.IsDBNull(r.GetOrdinal("Lisätiedot")) ? "" : r.GetString("Lisätiedot");

                                r.Close(); // Suljetaan lukija ennen rivien hakua
                                l.Tuotteet = HaeTuotteetLaskulle(id);
                                return l;
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Laskun haku epäonnistui: " + ex.Message); }
            }
            return null;
        }
        public static bool PaivitaLasku(Lasku lasku)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    // 1. Päivitetään laskun perustiedot
                    string sql = @"UPDATE Lasku SET Päiväys=@p, Eräpäivä=@ep, AsiakasNimi=@an, AsiakasOsoite=@ao, 
                   AsiakasPosti=@ap, Lisätiedot=@lisa WHERE LaskunNumero=@id";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p", lasku.Päiväys);
                    cmd.Parameters.AddWithValue("@ep", lasku.Eräpäivä);
                    cmd.Parameters.AddWithValue("@an", lasku.AsiakasInfo.Nimi);
                    cmd.Parameters.AddWithValue("@ao", lasku.AsiakasInfo.Osoite);
                    cmd.Parameters.AddWithValue("@ap", lasku.AsiakasInfo.Postinumero);
                    cmd.Parameters.AddWithValue("@lisa", lasku.AsiakasInfo.Lisätiedot ?? "");
                    cmd.Parameters.AddWithValue("@id", lasku.LaskunNumero);
                    cmd.ExecuteNonQuery();

                    // 2. Päivitetään rivit (poistetaan vanhat ja lisätään uudet)
                    new MySqlCommand($"DELETE FROM Laskurivi WHERE lasku_id={lasku.LaskunNumero}", conn).ExecuteNonQuery();

                    string rowSql = @"INSERT INTO Laskurivi (lasku_id, tuote_id, nimi, määrä, yksikkö, a_hinta, alv) 
                      VALUES (@lid, @tid, @n, @m, @y, @h, @a)";

                    foreach (var rivi in lasku.Tuotteet)
                    {
                        // --- UUSI LOGIIKKA ALKAA ---
                        if (rivi.Tuote_ID == 0 && !string.IsNullOrWhiteSpace(rivi.Nimi))
                        {
                            string insertTuoteSql = "INSERT INTO Tuote (nimi, määrä, yksikkö, a_hinta, alv) VALUES (@tn, 0, @ty, @th, @ta)";
                            using (MySqlCommand tCmd = new MySqlCommand(insertTuoteSql, conn))
                            {
                                tCmd.Parameters.AddWithValue("@tn", rivi.Nimi);
                                tCmd.Parameters.AddWithValue("@ty", rivi.Yksikkö);
                                tCmd.Parameters.AddWithValue("@th", rivi.A_Hinta);
                                tCmd.Parameters.AddWithValue("@ta", rivi.ALV);
                                tCmd.ExecuteNonQuery();

                                rivi.Tuote_ID = (int)tCmd.LastInsertedId;
                            }
                        }
                        // --- UUSI LOGIIKKA PÄÄTTYY ---

                        MySqlCommand rCmd = new MySqlCommand(rowSql, conn);
                        rCmd.Parameters.AddWithValue("@lid", lasku.LaskunNumero);
                        rCmd.Parameters.AddWithValue("@tid", rivi.Tuote_ID == 0 ? (object)DBNull.Value : rivi.Tuote_ID);
                        rCmd.Parameters.AddWithValue("@n", rivi.Nimi);
                        rCmd.Parameters.AddWithValue("@m", rivi.Määrä);
                        rCmd.Parameters.AddWithValue("@y", rivi.Yksikkö);
                        rCmd.Parameters.AddWithValue("@h", rivi.A_Hinta);
                        rCmd.Parameters.AddWithValue("@a", rivi.ALV);
                        rCmd.ExecuteNonQuery();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Päivitysvirhe: " + ex.Message);
                    return false;
                }
            }
        }
        public static ObservableCollection<Lasku> HaeNimella(string nimi)
        {
            var lista = new ObservableCollection<Lasku>();
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM Lasku WHERE AsiakasNimi LIKE @nimi";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandText = "SELECT * FROM Lasku WHERE AsiakasNimi LIKE @alku OR AsiakasNimi LIKE @vali";

                        // "Matti%" -> Löytää "Matti Meikäläinen"
                        cmd.Parameters.AddWithValue("@alku", nimi + "%");

                        // "% Meikäläinen%" -> Löytää "Matti Meikäläinen" (huomaa välilyönti alussa)
                        cmd.Parameters.AddWithValue("@vali", "% " + nimi + "%");

                        using (MySqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var l = new Lasku
                                {
                                    LaskunNumero = r.GetInt32("LaskunNumero"),
                                    Päiväys = r.GetDateTime("Päiväys"),
                                    Eräpäivä = r.GetDateTime("Eräpäivä"),
                                };
                                l.AsiakasInfo.Nimi = r.GetString("AsiakasNimi");
                                l.AsiakasInfo.Osoite = r.GetString("AsiakasOsoite");
                                l.AsiakasInfo.Postinumero = r.GetString("AsiakasPosti");
                                lista.Add(l);
                            }
                        }
                    }
                    // Haetaan rivit jokaiselle löytyneelle laskulle
                    foreach (var lasku in lista)
                    {
                        lasku.Tuotteet = HaeTuotteetLaskulle(lasku.LaskunNumero);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Haku virhe nimen perusteella: " + ex.Message); }
            }
            return lista;
        }
    }
}