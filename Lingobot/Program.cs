using Discord;
using Discord.WebSocket;

namespace LingoBot
{
    class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
               
        List<string> Words = new List<string>();

        // Client starten, instellen en inloggen
        public async Task MainAsync()                                          
        {
            _client = new DiscordSocketClient();
            _client.MessageReceived += CommandHandler;
            _client.Log += Log;

            var token = "token"; //Discord bot inlog token

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        // Logberichten doorzetten naar console
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // CommandHandler wordt aangeroepen wanneer de bot berichten ontvangt (privé of in aangesloten servers)
        private Task CommandHandler(SocketMessage message)
        {
            string command = "";
            int lengthOfCommand = -1;

            // Bots negeren
            if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            // Commando's (beginnend met !) verwerken
            if (message.Content.StartsWith("!"))
            {
                // Lengte van commando vaststellen en uit bericht isoleren
                if (message.Content.Contains(' '))
                {
                    lengthOfCommand = message.Content.IndexOf(' ');
                }
                else
                {
                    lengthOfCommand = message.Content.Length;
                }

                command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

                if (command == "ping")
                {
                    message.Channel.SendMessageAsync("pong!");
                    return Task.CompletedTask;
                }
                // Als commando !hallo of !hoi
                if (command == "hallo" || command == "hoi")
                {
                    message.Channel.SendMessageAsync("Hallo "+ message.Author.Mention + ", ik ben Lingobot! Zin om een potje lingo te spelen?" + Environment.NewLine +
                                                    "Type !lingo in chat, dan stuur ik je een PB!" + Environment.NewLine +
                                                    "Type !help om te zien welke andere commando's ik ken.");
                    
                    return Task.CompletedTask;
                }
                // Als commando !help
                if (command == "help")
                {
                    message.Channel.SendMessageAsync("!hallo/hoi - Zeg hoi en krijg een berichtje terug!" + Environment.NewLine +
                                                    "!help - Lijst met beschikbare commando's zien" + Environment.NewLine +
                                                    "!lingo - Speel een potje lingo met mij!" + Environment.NewLine +
                                                    "!stop - Stop het spelletje lingo :(" + Environment.NewLine +
                                                    "!stats - Bekijk je statistieken");

                    return Task.CompletedTask;
                }

                // Als commando !lingo
                if (command == "lingo")
                {
                    // Kijk of speler bestaat in systeem, zo niet maak bestand aan
                    if (!Player.CheckForPlayer(message.Author.Id))
                    {
                        Player.AddPlayer(message);
                    }

                    // Kijk of speler al een spel speelt, 
                    if (Player.IsGameActive(message.Author.Id))
                    {
                        message.Author.SendMessageAsync("Al een spel bezig, doe een gok!" + Environment.NewLine + Player.ReadGuesses(message.Author.Id));
                    }
                    // zo niet start spel en pas waardes aan in spelersbestand
                    else
                    {                        
                        Player.SetAnswer(message.Author.Id, StartNewGame());
                        Player.ActivateGame(message.Author.Id);
                        message.Author.SendMessageAsync("Spel gestart! Doe een gok door eerst ? en dan je 5 letter woord te typen. " + message.Author.Mention);                        
                    }
                    return Task.CompletedTask;
                }
                // Als commando !stop
                if (command == "stop")
                {
                    // Checken of spel actief is, zo ja spel stoppen en spelersbestand aanpassen
                    if (Player.IsGameActive(message.Author.Id))
                    {
                        // Punten alleen aftrekken als er is gegokt
                        if (Player.GuessCount(message.Author.Id) > 0)
                        {
                            Player.AddLoss(message.Author.Id);
                        }
                        Player.DeactivateGame(message.Author.Id);
                        Player.ClearGuesses(message.Author.Id);
                        message.Author.SendMessageAsync("Spel is gestopt");
                    }
                    else
                    {
                        message.Author.SendMessageAsync("Geen spel actief");


                    }
                    return Task.CompletedTask;
                }
                // Als commando !stats, dan statistieken van commando sturende speler laten zien
                if (command == "stats")
                {
                    message.Channel.SendMessageAsync(Player.ShowStats(message.Author.Id));

                    return Task.CompletedTask;
                }
                // Als commando onbekend
                else
                {
                    message.Channel.SendMessageAsync("Dit commando ken ik niet, Type !help om te zien wat ik wel kan!");
                }
            }

            // Gokken verwerken
            if (message.Content.StartsWith("?"))
            {   
                // Checken of spel actief is
                if (!Player.IsGameActive(message.Author.Id))
                {
                    message.Author.SendMessageAsync("Er is geen spel bezig, typ !lingo om een spel te beginnen.");
                    return Task.CompletedTask;
                }
                // Checken of gok niet te lang of the kort is
                if (message.Content.Length != 6)
                {
                    message.Author.SendMessageAsync("Je gok moet uit 5 letters bestaan, probeer opnieuw!");
                    return Task.CompletedTask;
                }

                var guess = message.Content.Substring(1, 5).ToLower();
                var answer = Player.ReadAnswer(message.Author.Id);

                // Checken of gok uit alleen letters bestaat
                if (!guess.All(c => Char.IsLetter(c)))
                {
                    message.Author.SendMessageAsync("Gok mag alleen uit letters bestaan.");
                    return Task.CompletedTask;
                }
                // Checken of geldig woord
                if (!Words.Contains(guess))
                {
                    message.Author.SendMessageAsync(guess + " is geen geldig woord.");
                    return Task.CompletedTask;
                }

                // Als speler minder dan 7 keer heeft gegokt, gok verwerken
                if (Player.GuessCount(message.Author.Id) < 7)
                {
                    string returnLine = "";
                    string G = ":green_square:";
                    string Y = ":yellow_square:";
                    string B = ":black_large_square:";

                    // Als antwoord goed is spel winnen, stoppen en spelersbestand aanpassen
                    if (guess == answer)
                    {
                        message.Author.SendMessageAsync(Player.ReadGuesses(message.Author.Id) + G+G+G+G+G + " " + guess + Environment.NewLine + "Dat is goed, je hebt gewonnen!:tada:");

                        Player.AddWin(message.Author.Id);
                        Player.DeactivateGame(message.Author.Id);
                        Player.ClearGuesses(message.Author.Id);

                        return Task.CompletedTask;
                    }

                    // Gok en antwoord in letters splitsen
                    char a = answer[0];
                    char b = answer[1];
                    char c = answer[2];
                    char d = answer[3];
                    char e = answer[4];

                    char aa = guess[0];
                    char bb = guess[1];
                    char cc = guess[2];
                    char dd = guess[3];
                    char ee = guess[4];

                    // Checken per letter of deze goed is, ergens anders in woord voorkomt of gewoon fout is
                    // Voegt aan de hand van correctheid een zwart, geel of groen blokje toe aan de returnLine
                    //
                    // Eerste letter
                    if (aa == a)
                    {
                        returnLine += G;
                    }
                    else if (answer.Contains(aa))
                    {
                        returnLine += Y;
                    }
                    else
                    {
                        returnLine += B;
                    }

                    // Tweede letter
                    if (bb == b)
                    {
                        returnLine += G;
                    }
                    else if (answer.Contains(bb))
                    {
                        returnLine += Y;
                    }
                    else
                    {
                        returnLine += B;
                    }

                    // Derde letter
                    if (cc == c)
                    {
                        returnLine += G;
                    }
                    else if (answer.Contains(cc))
                    {
                        returnLine += Y;
                    }
                    else
                    {
                        returnLine += B;
                    }

                    // Vierde letter
                    if (dd == d)
                    {
                        returnLine += G;
                    }
                    else if (answer.Contains(dd))
                    {
                        returnLine += Y;
                    }
                    else
                    {
                        returnLine += B;
                    }

                    // Vijfde letter
                    if (ee == e)
                    {
                        returnLine += G;
                    }
                    else if (answer.Contains(ee))
                    {
                        returnLine += Y;
                    }
                    else
                    {
                        returnLine += B;
                    }

                    // Regel maken, deze aan vorige regels toevoegen en alle regels sturen in chat
                    var guessLine = returnLine + " " + guess + Environment.NewLine;
                    Player.AddGuess(message.Author.Id, guessLine);
                    message.Author.SendMessageAsync(Player.ReadGuesses(message.Author.Id));


                    // Als speler 7 keer heeft gegokt, verliezen
                    if (Player.GuessCount(message.Author.Id) >= 7)
                    {
                        message.Author.SendMessageAsync("Het antwoord was " + answer);

                        Player.AddLoss(message.Author.Id);
                        Player.DeactivateGame(message.Author.Id);
                        Player.ClearGuesses(message.Author.Id);
                    }
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
            } 
            return Task.CompletedTask;
        }

        // Nieuw spel starten
        private string StartNewGame()
        {
            // Woorden.txt regel voor regel uitlezen en in list verwerken
            Words = File.ReadAllLines("Woorden.txt").ToList();

            // Willekeurig woord kiezen, antwoord naar console schrijven en terugsturen voor opslaan in spelersbestand 
            Random numberGen = new Random();            
            int random = numberGen.Next(0, Words.Count);
            var randomAnswer = (Words[random].ToLower());
            
            Console.WriteLine("New game started, answer is " + randomAnswer);

            return randomAnswer;
        }
    } 

    // Custom class voor het opslaan van spelers
    public class Player
    {
        public static List<Player> players = new List<Player>();

        public ulong Id { get; set; }
        public string Name { get; set; }
        public int Wins { get; set; }
        public int Lost { get; set; }
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public bool ActiveGame { get; set; }
        public int Guesses { get; set; }
        public string? Answer { get; set; }
        public string? LastGame { get; set; }

        // Checken of spelersbestand al bestaat dmv zoeken naar unieke Discord userId
        public static bool CheckForPlayer(ulong authorId)
        {
            return players.Any(p => p.Id == authorId);
        }
        // Nieuw spelersbestand aanmaken
        public static void AddPlayer(SocketMessage message)
        {
            var newPlayer = new Player
            {
                Id = message.Author.Id,
                Name = message.Author.Username,
                Wins = 0,
                Lost = 0,
                Streak = 0,
                LongestStreak = 0,
                ActiveGame = false, 
                Guesses = 0, 
                Answer = " ",
                LastGame = " "
            };
            players.Add(newPlayer);
            Console.WriteLine("Player added");
        }
        // Voeg win toe aan spelerbestand
        public static void AddWin(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].Wins++;
            players[userIndex].Streak++;

            if (players[userIndex].Streak > players[userIndex].LongestStreak)
            {
                players[userIndex].LongestStreak = players[userIndex].Streak;
            }
        }
        // Voeg verlies toe aan spelerbestand
        public static void AddLoss(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].Lost++;

            players[userIndex].Streak = 0;
        }
        // Game 'activeren' in spelersbestand
        public static void ActivateGame(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].ActiveGame = true;
        }
        // Game 'deactiveren' in spelersbestand
        public static void DeactivateGame(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].ActiveGame = false;
        }
        // Check of game actief is voor speler
        public static bool IsGameActive(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            return players[userIndex].ActiveGame;
        }
        // Voeg laatste gok toe aan spelersbestand, zowel in tekst als hoeveelheid
        public static void AddGuess(ulong authorId, string guessLine)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].Guesses++;
            players[userIndex].LastGame += guessLine;
        }
        // Lees alle gokken van laatste spel van speler
        public static string ReadGuesses(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            return players[userIndex].LastGame;
        }
        // Lees aantal gokken van speler
        public static int GuessCount(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            return players[userIndex].Guesses;
        }
        // Gokken verwijderen, zowel tekst als hoeveelheid
        public static void ClearGuesses(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].Guesses = 0;
            players[userIndex].LastGame = " ";
        }
        // Antwoord voor actieve spel van speler schrijven naar spelersbestand
        public static void SetAnswer(ulong authorId, string answer)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            players[userIndex].Answer = answer;
        }
        // Antwoord voor actieve spel van speler uitlezen
        public static string ReadAnswer(ulong authorId)
        {
            var userIndex = players.FindIndex(p => p.Id == authorId);

            return players[userIndex].Answer;
        }
        // Statistieken speler laten zien.
        public static string ShowStats(ulong authorId)
        {
            // Check of speler een bestand heeft
            if (!CheckForPlayer(authorId))
            {
                return "Je hebt nog niet gespeeld, type !lingo om te spelen!";
            }
            
            // Zo ja, gegevens verzamelen, verwerken in string, terugsturen
            var userIndex = players.FindIndex(p => p.Id == authorId);
            var name = players[userIndex].Name;
            var wins = players[userIndex].Wins;
            var lost = players[userIndex].Lost;
            var streak = players[userIndex].Streak;
            var longestStreak = players[userIndex].LongestStreak;

            var returnLine = "Naam: " + name + Environment.NewLine +
                             "Gewonnen: " + wins + Environment.NewLine +
                             "Verloren: " + lost + Environment.NewLine +
                             "Zegereeks: " + streak + Environment.NewLine +
                             "Record reeks: " + longestStreak;

            return returnLine;
        }
    }
}