// Localization for UltraShield: English and Serbian (Latin script).
//
// How it works: the English text is the key. L.T("Open") returns the Serbian text when Serbian is on,
// and the English text otherwise (also for anything not translated yet, so nothing ever shows a blank).
// L.Apply(window) walks the window and translates every fixed label (Content, Header, Text, Title,
// tooltips and screen-reader names) in place; it can be called again after switching the language,
// so the switch is instant, no restart. Values that come from bindings are never touched.
// Inside DataTemplates use {loc:T 'English text'} in XAML.
//
// The choice is saved in %LOCALAPPDATA%\UltraShield\language.txt. Until the user picks one,
// the language follows Windows (Serbian Windows -> Serbian, anything else -> English).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace UltraShield
{
    public static partial class L
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UltraShield", "language.txt");

        private static Dictionary<string, string>? _toEnglish;

        public static bool Serbian { get; private set; }

        /// <summary>Raised after the language changes, so windows can refresh texts they build in code.</summary>
        public static event Action? Changed;

        static L()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    Serbian = File.ReadAllText(SettingsPath).Trim() == "sr";
                    return;
                }
            }
            catch { }
            Serbian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "sr";
        }

        public static string T(string english)
            => Serbian && Sr.TryGetValue(english, out var s) ? s : english;

        public static string F(string englishFormat, params object?[] args)
        {
            try { return string.Format(T(englishFormat), args); }
            catch (FormatException) { return string.Format(englishFormat, args); }
        }

        public static void SetLanguage(bool serbian)
        {
            Serbian = serbian;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, serbian ? "sr" : "en");
            }
            catch { }
            foreach (Window w in Application.Current.Windows) Apply(w);
            Changed?.Invoke();
        }

        /// <summary>Switches between English and Serbian and says so (the button that calls this keeps focus).</summary>
        public static void Toggle(Button languageButton)
        {
            SetLanguage(!Serbian);
            UpdateLanguageButton(languageButton);
            var peer = UIElementAutomationPeer.FromElement(languageButton) ?? UIElementAutomationPeer.CreatePeerForElement(languageButton);
            peer?.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent,
                Serbian ? "Jezik: srpski" : "Language: English", "UltraLanguage");
        }

        /// <summary>The language button always shows the OTHER language, written in that language.</summary>
        public static void UpdateLanguageButton(Button b)
        {
            b.Content = Serbian ? "English" : "Srpski";
            AutomationProperties.SetName(b, Serbian ? "Switch to English (jezik: srpski)" : "Prebaci na srpski (language: English)");
        }

        private static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (_toEnglish == null)
            {
                _toEnglish = new Dictionary<string, string>();
                foreach (var kv in Sr) _toEnglish[kv.Value] = kv.Key;
            }
            string english = _toEnglish.TryGetValue(text, out var en) ? en : text;
            return T(english);
        }

        // Translates one property only when it holds a plain local string (not a binding or resource).
        private static void Fix(DependencyObject o, DependencyProperty p)
        {
            if (o.ReadLocalValue(p) is string s && !BindingOperations.IsDataBound(o, p))
            {
                var t = Translate(s);
                if (!ReferenceEquals(t, s) && t != s) o.SetValue(p, t);
            }
        }

        /// <summary>Translates every fixed label under <paramref name="root"/> to the current language.</summary>
        public static void Apply(DependencyObject root)
        {
            var seen = new HashSet<DependencyObject>();
            Walk(root, seen);
        }

        private static void Walk(DependencyObject o, HashSet<DependencyObject> seen)
        {
            if (!seen.Add(o)) return;

            Fix(o, AutomationProperties.NameProperty);
            Fix(o, AutomationProperties.HelpTextProperty);
            Fix(o, FrameworkElement.ToolTipProperty);
            switch (o)
            {
                case Window w: Fix(w, Window.TitleProperty); break;
                case TextBlock tb: Fix(tb, TextBlock.TextProperty); break;
                case HeaderedContentControl hc: Fix(hc, HeaderedContentControl.HeaderProperty); break;
                case HeaderedItemsControl hi: Fix(hi, HeaderedItemsControl.HeaderProperty); break;
            }
            if (o is ContentControl cc && o is not Window) Fix(cc, ContentControl.ContentProperty);
            if (o is ListView lv && lv.View is GridView gv)
                foreach (var col in gv.Columns) Fix(col, GridViewColumn.HeaderProperty);

            // Logical children (also reaches things not yet shown, like other tabs) ...
            foreach (var child in LogicalTreeHelper.GetChildren(o))
                if (child is DependencyObject d) Walk(d, seen);
            // ... and visual children (reaches the inside of controls such as UserControls and templates).
            if (o is System.Windows.Media.Visual || o is System.Windows.Media.Media3D.Visual3D)
            {
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(o);
                for (int i = 0; i < n; i++) Walk(System.Windows.Media.VisualTreeHelper.GetChild(o, i), seen);
            }
        }
    }

    /// <summary>XAML: {loc:T 'English text'} - for texts inside DataTemplates.</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class TExtension : MarkupExtension
    {
        public TExtension() { }
        public TExtension(string text) { Text = text; }
        [ConstructorArgument("text")] public string Text { get; set; } = "";
        public override object ProvideValue(IServiceProvider serviceProvider) => L.T(Text);
    }

    public static partial class L
    {
        // English text -> Serbian (Latin script).
        private static readonly Dictionary<string, string> Sr = new()
        {
            ["Theme:"] = "Tema:",
            ["Theme selector"] = "Izbor teme",
            ["Light"] = "Svetla",
            ["Dark"] = "Tamna",
            ["High Contrast"] = "Visok kontrast",
            ["About"] = "O programu",
            ["Close"] = "Zatvori",
            ["Close about dialog"] = "Zatvori prozor O programu",
            ["How to support this project"] = "Kako podržati projekat",
            ["Created by Demir Ajvazi"] = "Autor: Demir Ajvazi",
            ["Version 0.1 (early skeleton)"] = "Verzija 0.1 (rana osnovna verzija)",
            ["Ready."] = "Spremno.",
            ["Digital self-defense - education, scanning, and hardening. On-demand, not real-time noise."] = "Digitalna samoodbrana - edukacija, skeniranje i zaštita sistema. Na zahtev, bez stalne buke u pozadini.",
            ["About UltraShield"] = "O programu UltraShield",
            ["Education"] = "Edukacija",
            ["Education module"] = "Modul edukacije",
            ["Scanner"] = "Skener",
            ["Scanner module"] = "Modul skenera",
            ["Checklist"] = "Kontrolna lista",
            ["Hardening checklist module"] = "Modul kontrolne liste za zaštitu",
            ["Version 1.0"] = "Verzija 1.0",
            ["Digital self-defense - education, package/file/folder scanning, and security hardening. On-demand, not real-time noise. Part of the Ultra suite of accessible software."] = "Digitalna samoodbrana - edukacija, skeniranje paketa, fajlova i foldera i zaštita sistema. Na zahtev, bez stalne buke u pozadini. Deo Ultra porodice pristupačnih programa.",
            ["An unexpected error occurred and has been logged. UltraShield will try to continue running."] = "Došlo je do neočekivane greške i ona je zapisana. UltraShield će pokušati da nastavi rad.",
            ["UltraShield - Error"] = "UltraShield - greška",
            ["Security hardening checklist"] = "Kontrolna lista za zaštitu",
            ["Checklist progress"] = "Napredak kontrolne liste",
            ["{0} of {1} completed"] = "Završeno {0} od {1}",
            ["2FA enabled on primary email"] = "Dvostepena prijava (2FA) uključena na glavnom mejlu",
            ["2FA enabled on GitHub / package registry accounts"] = "Dvostepena prijava (2FA) uključena na GitHub nalogu i nalozima za pakete (npm, PyPI)",
            ["Dependencies audited for known-malicious packages"] = "Zavisnosti proverene na poznate zlonamerne pakete",
            ["Recent, tested backups exist"] = "Postoje skorašnje, proverene rezervne kopije",
            ["No plaintext credentials in repos or scripts"] = "Nema lozinki ni ključeva u običnom tekstu u repozitorijumima i skriptama",
            ["Verified recruiter/job offers through an official company channel before running any code they sent"] = "Ponude za posao i regrutere proveravam preko zvaničnog kanala firme pre nego što pokrenem bilo kakav kod koji su poslali",
            ["Browser extensions reviewed - none installed from outside the official store"] = "Proverena proširenja pregledača - nijedno nije instalirano van zvanične prodavnice",
            ["Threat topics"] = "Teme o pretnjama",
            ["Topic details"] = "Detalji teme",
            ["Fake recruiter / job offer scams"] = "Prevare sa lažnim regruterima i ponudama za posao",
            ["Trojanized npm / PyPI packages"] = "Zaraženi npm i PyPI paketi",
            ["Fake crypto exchanges and wallets"] = "Lažne kripto menjačnice i novčanici",
            ["Fake IT worker / video call scams"] = "Prevare sa lažnim IT radnicima i video pozivima",
            ["Malicious browser extensions"] = "Zlonamerna proširenja pregledača",
            ["EDU_RECRUITER"] = "Grupe povezane sa državama, kao što je Lazarus, često stupaju u kontakt preko LinkedIn-a, Discord-a ili mejla, predstavljajući se kao regruteri za blokčejn, fintek ili AI startap. Posle malo prijatnog dopisivanja, od mete traže da uradi „test programiranja“ ili „demo projekat“ - obično mali repozitorijum koji treba klonirati i pokrenuti. Taj repozitorijum je zapravo napad: njegovo pokretanje (pa čak i samo korak instalacije) pokreće malver koji krade lozinke ili otvara daljinski pristup računaru programera.\n\nŠta da radiš: nikad ne pokreći kod koji ti pošalju u okviru razgovora za posao na svom glavnom računaru. Koristi virtuelnu mašinu ili izolovano okruženje, a firmu i identitet regrutera proveri preko zvaničnog sajta firme ili njene stranice za karijeru - ne samo preko profila koji ti je pisao.",
            ["EDU_PACKAGES"] = "Ovo je postao jedan od najaktivnijih načina napada na programere. Taktike zabeležene do 2026. uključuju: objavljivanje paketa sa imenima sličnim popularnim, pouzdanim alatima („brendžeking“ - npr. lažni polyfill ili pomoćni paket koji zvuči kao nešto što bi instalirao bez razmišljanja); preuzimanje naloga pravog održavaoca i ubacivanje zlonamernog koda u ažuriranje koje izgleda rutinski; i napade koji se aktiviraju tek kad se paket učita i koristi, a ne pri instalaciji, upravo da bi zaobišli zaštitu instalacionih skripti.\n\nŠta da radiš: pre instaliranja bilo čega nepoznatog proveri istoriju objavljivanja paketa, broj održavalaca i link ka repozitorijumu - i koristi karticu Skener u ovom programu kao prvu proveru.",
            ["EDU_CRYPTO"] = "Domeni koji liče na prave i kopirani interfejsi menjačnica i novčanika koriste se da bi se direktno ukrali podaci za prijavu, seed fraze ili privatni ključevi. Malver koji stiže preko dve pretnje iznad često je napravljen baš da pretraži računar žrtve u potrazi za fajlovima novčanika (MetaMask, Exodus, Atomic itd.) i da ih automatski pošalje napadaču.\n\nŠta da radiš: sačuvaj u obeleživačima menjačnice i novčanike koje stvarno koristiš umesto da ih otvaraš preko rezultata pretrage ili linkova, i nikad ne kucaj seed frazu nigde osim u sam program novčanika.",
            ["EDU_ITWORKER"] = "Srodna, ali drugačija prevara: napadači se predstavljaju kao kandidati za posao na daljinu ili saradnici (ponekad uz lažni video i glas napravljen veštačkom inteligencijom ili ukradeni identitet) da bi se zaposlili u pravim firmama zbog plate ili da bi dobili pristup internim sistemima i kodu. Odvojeno od toga, lažni video pozivi u kojima se neko predstavlja kao direktor koriste se da se zaposleni natera na hitne, neodobrene uplate ili predaju lozinki.\n\nŠta da radiš: neobične ili hitne zahteve - posebno one u vezi sa novcem, lozinkama ili pristupom - pre bilo čega proveri preko drugog, nezavisnog kanala.",
            ["EDU_EXTENSIONS"] = "Lažna ili zaražena proširenja pregledača (ponekad deljena van zvaničnih prodavnica, a ponekad provučena i kroz zvaničnu proveru) koriste se za ubacivanje zlonamernog koda, presretanje sadržaja međuspremnika ili tiho menjanje kripto transakcija u pregledaču.\n\nŠta da radiš: instaliraj proširenja samo iz zvanične prodavnice, proveri izdavača i broj ocena, i povremeno pregledaj šta imaš instalirano - za to postoji stavka na kartici Kontrolna lista.",
            ["Package / file / folder scanner"] = "Skener paketa, fajlova i foldera",
            ["Checks packages against npm/PyPI and known-malicious lists, or checks a file or an entire folder's hashes against MalwareBazaar and (optionally) VirusTotal. This is an on-demand scan, not real-time protection - like a Malwarebytes scan, it only looks when you run it."] = "Proverava pakete preko npm/PyPI i lista poznatih zlonamernih paketa, ili proverava heševe fajla ili celog foldera u bazi MalwareBazaar i (po želji) VirusTotal. Ovo je skeniranje na zahtev, a ne zaštita u realnom vremenu - kao Malwarebytes skeniranje, gleda samo kad ga pokreneš.",
            ["What are you checking?"] = "Šta proveravaš?",
            ["npm package"] = "npm paket",
            ["Check an npm package"] = "Proveri npm paket",
            ["PyPI package"] = "PyPI paket",
            ["Check a PyPI package"] = "Proveri PyPI paket",
            ["Single file"] = "Jedan fajl",
            ["Check a single local file"] = "Proveri jedan fajl na računaru",
            ["Full folder scan"] = "Skeniranje celog foldera",
            ["Scan an entire folder"] = "Skeniraj ceo folder",
            ["Package name, file path, or folder path:"] = "Ime paketa, putanja fajla ili foldera:",
            ["Package name, file path, or folder path"] = "Ime paketa, putanja fajla ili foldera",
            ["Browse..."] = "Pretraži...",
            ["Browse for a file or folder"] = "Potraži fajl ili folder",
            ["Scan"] = "Skeniraj",
            ["Run scan"] = "Pokreni skeniranje",
            ["Result"] = "Rezultat",
            ["Scan result"] = "Rezultat skeniranja",
            ["Start scan"] = "Počni skeniranje",
            ["Start folder scan"] = "Počni skeniranje foldera",
            ["Cancel"] = "Otkaži",
            ["Cancel folder scan"] = "Otkaži skeniranje foldera",
            ["Folder scan progress"] = "Napredak skeniranja foldera",
            ["Folder scan status"] = "Stanje skeniranja foldera",
            ["Findings"] = "Pronađeno",
            ["Quarantine"] = "Karantin",
            ["Quarantine this file"] = "Premesti ovaj fajl u karantin",
            ["VirusTotal API key (optional)"] = "VirusTotal API ključ (opciono)",
            ["MalwareBazaar needs no key. VirusTotal needs your own free key from virustotal.com - UltraShield never ships a shared key, and yours is stored only on this machine. Folder scans respect VirusTotal's free-tier rate limit automatically."] = "Za MalwareBazaar ne treba ključ. Za VirusTotal treba tvoj besplatan ključ sa virustotal.com - UltraShield nikad ne isporučuje zajednički ključ, a tvoj se čuva samo na ovom računaru. Skeniranje foldera samo poštuje ograničenje broja upita besplatnog VirusTotal naloga.",
            ["VirusTotal API key"] = "VirusTotal API ključ",
            ["Save key"] = "Sačuvaj ključ",
            ["Save VirusTotal API key"] = "Sačuvaj VirusTotal API ključ",
            ["No scan run yet."] = "Skeniranje još nije pokrenuto.",
            ["VirusTotal API key saved locally."] = "VirusTotal API ključ je sačuvan na ovom računaru.",
            ["Select a folder to scan"] = "Izaberi folder za skeniranje",
            ["Select a file to check"] = "Izaberi fajl za proveru",
            ["Enter a package name or file path first."] = "Prvo upiši ime paketa ili putanju fajla.",
            ["Scanning..."] = "Skeniram...",
            ["Scan failed: {0}"] = "Skeniranje nije uspelo: {0}",
            ["That file path doesn't exist."] = "Ta putanja fajla ne postoji.",
            ["Add a VirusTotal API key below for a second opinion from ~70 antivirus engines."] = "Dodaj VirusTotal API ključ ispod za drugo mišljenje od oko 70 antivirusnih programa.",
            ["Choose a valid folder first."] = "Prvo izaberi postojeći folder.",
            ["Starting scan..."] = "Počinjem skeniranje...",
            ["Scanned {0} of {1} files - {2} threat(s) found so far. Checking: {3}"] = "Skenirano {0} od {1} fajlova - do sada pronađenih pretnji: {2}. Proveravam: {3}",
            ["Scan complete - no threats found."] = "Skeniranje je završeno - pretnje nisu pronađene.",
            ["Scan complete - {0} threat(s) found. Review the list below."] = "Skeniranje je završeno - pronađenih pretnji: {0}. Pregledaj listu ispod.",
            ["Scan cancelled."] = "Skeniranje je otkazano.",
            ["{0} package '{1}': {2}"] = "{0} paket „{1}“: {2}",
            ["Found in the registry."] = "Pronađen u registru.",
            ["Not found in the registry."] = "Nije pronađen u registru.",
            ["Warning: {0}"] = "Upozorenje: {0}",
            ["Threat label: {0}"] = "Oznaka pretnje: {0}",
            ["Detections: {0}/{1}"] = "Otkrivanja: {0}/{1}",
            ["Unknown"] = "Nepoznato",
            ["Clean"] = "Čisto",
            ["Suspicious"] = "Sumnjivo",
            ["Malicious"] = "Zlonamerno",
            ["Error"] = "Greška",
            ["Not quarantined"] = "Nije u karantinu",
            ["Quarantined"] = "U karantinu",
            ["Quarantine failed: {0}"] = "Premeštanje u karantin nije uspelo: {0}",
            ["Not present in MalwareBazaar's database. This does not guarantee the file is safe - it only means this specific database has no record of it."] = "Nema ga u bazi MalwareBazaar. To ne garantuje da je fajl bezbedan - znači samo da ova baza nema zapis o njemu.",
            ["Confirmed match in MalwareBazaar's public malware-sample database."] = "Potvrđeno poklapanje u javnoj bazi uzoraka malvera MalwareBazaar.",
            ["Unexpected response status: {0}"] = "Neočekivan odgovor servera: {0}",
            ["Lookup failed: {0}"] = "Provera nije uspela: {0}",
            ["Matches known-malicious package list: {0}"] = "Poklapa se sa listom poznatih zlonamernih paketa: {0}",
            ["Package not found in the npm registry."] = "Paket nije pronađen u npm registru.",
            ["No repository field in package metadata."] = "U podacima paketa nema linka ka repozitorijumu.",
            ["Package was first published only {0} day(s) ago."] = "Paket je prvi put objavljen pre samo {0} dana.",
            ["Package has only a single published version."] = "Paket ima samo jednu objavljenu verziju.",
            ["Heuristic checks only - this does not scan the package's actual code."] = "Samo heurističke provere - ovo ne skenira sam kod paketa.",
            ["Package not found on PyPI."] = "Paket nije pronađen na PyPI.",
            ["No homepage or project URLs in package metadata."] = "U podacima paketa nema početne stranice ni linkova projekta.",
            ["Package has only a single published release."] = "Paket ima samo jedno objavljeno izdanje.",
            ["No VirusTotal API key configured - add your own free key in Settings to enable this check."] = "VirusTotal API ključ nije podešen - dodaj svoj besplatan ključ da bi uključio ovu proveru.",
            ["Not present in VirusTotal's database."] = "Nema ga u bazi VirusTotal.",
            ["VirusTotal returned status {0}."] = "VirusTotal je vratio status {0}.",
            ["{0} of {1} engines flagged this file."] = "{0} od {1} antivirusnih programa označilo je ovaj fajl.",
            ["Quarantined file no longer exists."] = "Fajl iz karantina više ne postoji.",
        };
    }
}
