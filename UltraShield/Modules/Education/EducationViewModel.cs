using System.Collections.ObjectModel;
using UltraShield.Core;

namespace UltraShield.Modules.Education
{
    public class ThreatTopic
    {
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
    }

    public class EducationViewModel : ViewModelBase
    {
        public ObservableCollection<ThreatTopic> Topics { get; } = new()
        {
            new ThreatTopic
            {
                Title = "Fake recruiter / job offer scams",
                Body = "State-linked groups such as Lazarus commonly open contact through LinkedIn, " +
                       "Discord, or email, posing as a recruiter for a blockchain, fintech, or AI " +
                       "startup. After some friendly back-and-forth, the target is asked to complete " +
                       "a 'coding test' or 'demo project' - usually a small repository to clone and " +
                       "run. That repository is the payload: running it (or even just its install " +
                       "step) executes credential-stealing or remote-access malware on the developer's " +
                       "machine.\n\nWhat to do: never run code sent to you as part of a job process on " +
                       "your primary machine. Use a disposable VM or sandbox, and verify the company " +
                       "and the recruiter's identity through the company's own official website or " +
                       "careers page - not just the profile that messaged you."
            },
            new ThreatTopic
            {
                Title = "Trojanized npm / PyPI packages",
                Body = "This has become one of the most active attack paths against developers. " +
                       "Tactics documented through 2026 include: publishing packages with names close " +
                       "to popular, trusted tools ('brandjacking' - e.g. a fake polyfill or utility " +
                       "package that sounds like something you'd install without thinking twice); " +
                       "compromising a legitimate maintainer's account and slipping malicious code " +
                       "into a routine-looking update; and payloads that only fire when the package " +
                       "is imported and used, rather than at install time, specifically to dodge " +
                       "install-script security defaults.\n\nWhat to do: check a package's publish " +
                       "history, maintainer count, and repository link before installing anything " +
                       "unfamiliar - and use the Scanner tab in this app as a first-pass check."
            },
            new ThreatTopic
            {
                Title = "Fake crypto exchanges and wallets",
                Body = "Lookalike domains and cloned exchange or wallet interfaces are used to " +
                       "harvest login credentials, seed phrases, or private keys directly. Malware " +
                       "delivered through the two threats above is also frequently built specifically " +
                       "to scan a victim's machine for wallet files (MetaMask, Exodus, Atomic, etc.) " +
                       "and exfiltrate them automatically.\n\nWhat to do: bookmark exchanges/wallets you " +
                       "actually use rather than navigating via search results or links, and never " +
                       "type a seed phrase into anything other than the wallet software itself."
            },
            new ThreatTopic
            {
                Title = "Fake IT worker / video call scams",
                Body = "A related but distinct scheme: operatives pose as remote job applicants or " +
                       "contractors (sometimes using deepfaked video/audio or stolen identities) to get " +
                       "hired into real companies for a paycheck, or to gain insider access to internal " +
                       "systems and codebases. Separately, deepfaked video calls impersonating " +
                       "executives have been used to pressure employees into urgent, unauthorized " +
                       "payments or credential handovers.\n\nWhat to do: verify unusual or urgent " +
                       "requests - especially ones involving payments, credentials, or access - through " +
                       "a second, independent channel before acting."
            },
            new ThreatTopic
            {
                Title = "Malicious browser extensions",
                Body = "Fake or trojanized browser extensions (sometimes distributed outside official " +
                       "stores, sometimes slipping through official review) have been used to inject " +
                       "payloads, intercept clipboard content, or silently modify crypto transactions " +
                       "in the browser.\n\nWhat to do: only install extensions from the official store, " +
                       "check the publisher and review count, and periodically audit what's installed - " +
                       "there's an item for this on the Checklist tab."
            },
        };

        private ThreatTopic? _selectedTopic;
        public ThreatTopic? SelectedTopic
        {
            get => _selectedTopic;
            set => SetField(ref _selectedTopic, value);
        }

        public EducationViewModel()
        {
            SelectedTopic = Topics.Count > 0 ? Topics[0] : null;
        }
    }
}
