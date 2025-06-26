using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CyberSecurityBot
{
    public partial class MainWindow : Window
    {
        private string userName = "";
        private string userInterest = "";
        private readonly List<string> conversationLog = new List<string>();
        private readonly List<string> activityLog = new List<string>();
        private readonly Random rnd = new Random();
        private readonly List<TaskItem> tasks = new List<TaskItem>();
        private bool isQuizMode = false;
        private List<QuizQuestion> quizQuestions;
        private int currentQuizQuestionIndex = 0;
        private int quizScore = 0;

        // Dictionary to hold keyword responses for common cybersecurity topics.
        private readonly Dictionary<string, string[]> keywordResponses = new Dictionary<string, string[]>
        {
            { "password", new[] {
                "Use strong, unique passwords for every account.",
                "Avoid using personal information in your passwords.",
                "Enable two-factor authentication (2FA) where possible."
            }},
            { "phishing", new[] {
                "Phishing emails often create urgency. Don't fall for it.",
                "Double-check the sender's email address — scammers often spoof names.",
                "Never click links in suspicious emails or messages."
            }},
            { "privacy", new[] {
                "Review app permissions regularly and remove access you don't use.",
                "Use browsers that block trackers and ads for enhanced privacy.",
                "Limit personal details shared on social media platforms."
            }},
            { "scam", new[] {
                "If it sounds too good to be true, it probably is.",
                "Scams often involve urgent requests for money or personal info.",
                "Verify any suspicious communication by contacting the company directly."
            }}
        };

        // Represents a task item with properties for title, description, reminder date, and completion status.
        private class TaskItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime? ReminderDate { get; set; }
            public bool IsCompleted { get; set; }
        }
        // Represents a quiz question with its options, correct answer index, and explanation.
        private class QuizQuestion
        {
            public string Question { get; set; }
            public string[] Options { get; set; }
            public int CorrectAnswerIndex { get; set; }
            public string Explanation { get; set; }
        }
        // Constructor for the MainWindow class, initializes components and sets up the quiz questions and initial display.
        public MainWindow()
        {
            InitializeComponent();
            InitializeQuizQuestions();
            DisplayAsciiArt();
            GreetUser();
        }
        // Method to display ASCII art in the chat history.

        private void DisplayAsciiArt()
        {
            string asciiArt = @"
         ██████╗ ██╗   ██╗██████╗ ███████╗███████╗███████╗
        ██╔══██╗██║   ██║██╔══██╗██╔════╝██╔════╝██╔════╝
        ██████╔╝██║   ██║██████╔╝█████╗  █████╗  █████╗  
        ██╔═══╝ ██║   ██║██╔═══╝ ██╔══╝  ██╔══╝  ██╔══╝  
        ██║     ╚██████╔╝██║     ███████╗███████╗███████╗
        ╚═╝      ╚═════╝ ╚═╝     ╚══════╝╚══════╝╚══════╝


        [ PUPEEE, Your Cybersecurity Awareness Bot ]";
            AppendToChatHistory(asciiArt, Brushes.Cyan);
            LogActivity("Displayed ASCII art");
        }

        // Method to greet the user when the application starts.

        private void GreetUser()
        {
            AppendToChatHistory("+--------------------------------------------+", Brushes.Green);
            AppendToChatHistory("| Welcome to your CyberSecurity Assistant!  |", Brushes.Green);
            AppendToChatHistory("+--------------------------------------------+\n", Brushes.Green);
            AppendToChatHistory("What’s your name?", Brushes.Green);
            LogActivity("Greeted user");
        }

        // Event handler for the Send button click.
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string input = UserInput.Text.Trim().ToLower();
            UserInput.Text = "";

            if (string.IsNullOrWhiteSpace(input))
            {
                AppendToChatHistory("Please enter something so I can help you.", Brushes.Red);
                LogActivity("Empty input detected");
                return;
            }

            AppendToChatHistory($"> {input}", Brushes.White);
            conversationLog.Add(input);
            LogActivity($"User input: {input}");

            if (input.Contains("exit"))
            {
                AppendToChatHistory($"Goodbye {userName}! Stay safe online.", Brushes.Green);
                LogActivity("User exited");
                Close();
                return;
            }

            if (string.IsNullOrEmpty(userName))
            {
                userName = input;
                AppendToChatHistory($"\nNice to meet you, {userName}! Ask me anything about cybersecurity.", Brushes.Green);
                AppendToChatHistory("Topics: password, phishing, privacy, scam, quiz, task, activity log. Type 'exit' to quit.\n", Brushes.Green);
                LogActivity($"User set name: {userName}");
                return;
            }

            if (isQuizMode)
            {
                HandleQuizInput(input);
                return;
            }

            if (HandleTaskInput(input) || HandleActivityLogInput(input) || HandleQuizStart(input))
            {
                return;
            }

            if (input.Contains("worried") || input.Contains("scared"))
            {
                AppendToChatHistory($"It's okay to feel that way {userName}. You're not alone, and I'm here to guide you.", Brushes.Yellow);
                LogActivity("Detected worried/scared sentiment");
                return;
            }

            if (input.Contains("curious") || input.Contains("interested"))
            {
                AppendToChatHistory($"That's awesome {userName}! Curiosity is the first step to cybersecurity awareness.", Brushes.Yellow);
                LogActivity("Detected curious/interested sentiment");
                return;
            }

            if (input.Contains("interested in"))
            {
                var parts = input.Split(new[] { "interested in" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    userInterest = parts[1].Trim();
                    AppendToChatHistory($"Got it! I'll remember that you're interested in {userInterest}.", Brushes.Green);
                    LogActivity($"User set interest: {userInterest}");
                    return;
                }
            }

            if (input.Contains("what am i interested in"))
            {
                if (!string.IsNullOrWhiteSpace(userInterest))
                    AppendToChatHistory($"You told me you're interested in {userInterest}. ;-)", Brushes.Green);
                else
                    AppendToChatHistory($"Hey,{userName} you haven't told me your interest yet.", Brushes.Red);
                LogActivity("User queried interest");
                return;
            }

            bool matchedKeyword = false;
            foreach (var keyword in keywordResponses.Keys)
            {
                if (input.Contains(keyword))
                {
                    matchedKeyword = true;
                    string response = keywordResponses[keyword][rnd.Next(keywordResponses[keyword].Length)];
                    AppendToChatHistory(response, Brushes.Green);
                    LogActivity($"Responded to keyword: {keyword}");
                    break;
                }
            }

            if (!matchedKeyword)
            {
                AppendToChatHistory($"Hmm, I’m not sure I understand that {userName}. Can you rephrase or ask about cybersecurity?", Brushes.Red);
                LogActivity("Unrecognized input");
            }
        }
        //logic for handling task input
        private bool HandleTaskInput(string input)
        {
            if (Regex.IsMatch(input, @"^\s*task\s*$", RegexOptions.IgnoreCase))
            {
                AppendToChatHistory("Task commands available:", Brushes.Green);
                AppendToChatHistory("- Add a task: 'add task [task name]'", Brushes.Green);
                AppendToChatHistory("- Set a reminder: 'remind me in X days' after adding a task", Brushes.Green);
                AppendToChatHistory("- View tasks: 'view tasks'", Brushes.Green);
                AppendToChatHistory("- Complete a task: 'complete task X' (e.g., complete task 1)", Brushes.Green);
                LogActivity("User queried task commands");
                return true;
            }
            else if (Regex.IsMatch(input, @"add\s+task", RegexOptions.IgnoreCase))
            {
                var match = Regex.Match(input, @"add\s+task\s+(.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string taskTitle = match.Groups[1].Value.Trim();
                    var task = new TaskItem { Title = taskTitle, Description = $"Complete {taskTitle}", IsCompleted = false };
                    tasks.Add(task);
                    AppendToChatHistory($"Task added: '{taskTitle}'. Would you like a reminder? (e.g., 'remind me in 3 days')", Brushes.Green);
                    LogActivity($"Task added: {taskTitle}");
                    return true;
                }
            }
            else if (Regex.IsMatch(input, @"remind\s+me\s+in\s+\d+\s+days", RegexOptions.IgnoreCase))
            {
                var match = Regex.Match(input, @"remind\s+me\s+in\s+(\d+)\s+days", RegexOptions.IgnoreCase);
                if (match.Success && tasks.Any())
                {
                    int days = int.Parse(match.Groups[1].Value);
                    var task = tasks[tasks.Count - 1];
                    task.ReminderDate = DateTime.Now.AddDays(days);
                    AppendToChatHistory($"Reminder set for '{task.Title}' on {task.ReminderDate:dd/MM/yyyy}.", Brushes.Green);
                    LogActivity($"Reminder set for task: {task.Title} on {task.ReminderDate:dd/MM/yyyy}");
                    return true;
                }
            }
            else if (Regex.IsMatch(input, @"view\s+tasks", RegexOptions.IgnoreCase))
            {
                if (tasks.Count == 0)
                {
                    AppendToChatHistory("No tasks added yet.", Brushes.Red);
                }
                else
                {
                    AppendToChatHistory("Your tasks:", Brushes.Green);
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        var task = tasks[i];
                        string status = task.IsCompleted ? "[Completed]" : task.ReminderDate.HasValue ? $"[Reminder: {task.ReminderDate:dd/MM/yyyy}]" : "";
                        AppendToChatHistory($"{i + 1}. {task.Title}: {task.Description} {status}", Brushes.Green);
                    }
                    LogActivity("User viewed tasks");
                }
                return true;
            }
            else if (Regex.IsMatch(input, @"complete\s+task\s+\d+", RegexOptions.IgnoreCase))
            {
                var match = Regex.Match(input, @"complete\s+task\s+(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    int taskIndex = int.Parse(match.Groups[1].Value) - 1;
                    if (taskIndex >= 0 && taskIndex < tasks.Count)
                    {
                        tasks[taskIndex].IsCompleted = true;
                        AppendToChatHistory($"Task '{tasks[taskIndex].Title}' marked as completed.", Brushes.Green);
                        LogActivity($"Task completed: {tasks[taskIndex].Title}");
                    }
                    else
                    {
                        AppendToChatHistory("Invalid task number.", Brushes.Red);
                    }
                    return true;
                }
            }
            return false;
        }

        // Logic for handling activity log input.
        private bool HandleActivityLogInput(string input)
        {
            if (Regex.IsMatch(input, @"^\s*(show\s+)?activity\s+log\s*$", RegexOptions.IgnoreCase))
            {
                if (activityLog.Count == 0)
                {
                    AppendToChatHistory("No activity recorded yet.", Brushes.Red);
                }
                else
                {
                    AppendToChatHistory("Recent activity log (last 5 actions):", Brushes.Green);
                    var recentLogs = activityLog.Skip(Math.Max(0, activityLog.Count - 5)).Take(5).ToList();
                    for (int i = 0; i < recentLogs.Count; i++)
                    {
                        AppendToChatHistory($"{i + 1}. {recentLogs[i]}", Brushes.Green);
                    }
                    LogActivity("User viewed activity log");
                }
                return true;
            }
            return false;
        }
        private bool HandleQuizStart(string input)
        {
            if (Regex.IsMatch(input, @"^\s*(start\s+)?quiz\s*$", RegexOptions.IgnoreCase))
            {
                isQuizMode = true;
                currentQuizQuestionIndex = 0;
                quizScore = 0;
                AppendToChatHistory("Starting Cybersecurity Quiz! Answer with the letter (e.g., 'A') or number of your choice.", Brushes.Green);
                DisplayQuizQuestion();
                LogActivity("Started quiz");
                return true;
            }
            return false;
        }
        // Logic for handling quiz input.
        private void HandleQuizInput(string input)
        {
            if (currentQuizQuestionIndex >= quizQuestions.Count)
            {
                isQuizMode = false;
                string feedback = quizScore >= 8 ? "Great job! You're a cybersecurity pro!" : "Keep learning to stay safe online!";
                AppendToChatHistory($"Quiz completed! Your score: {quizScore}/{quizQuestions.Count}. {feedback}", Brushes.Green);
                LogActivity($"Quiz completed with score: {quizScore}/{quizQuestions.Count}");
                return;
            }

            var currentQuestion = quizQuestions[currentQuizQuestionIndex];
            string normalizedInput = input.Trim().ToUpper();
            int selectedAnswer = -1;

            if (int.TryParse(input, out int numAnswer) && numAnswer >= 1 && numAnswer <= currentQuestion.Options.Length)
            {
                selectedAnswer = numAnswer - 1;
            }
            else if (normalizedInput.Length == 1 && "ABCD".Contains(normalizedInput))
            {
                selectedAnswer = "ABCD".IndexOf(normalizedInput);
            }

            if (selectedAnswer >= 0 && selectedAnswer < currentQuestion.Options.Length)
            {
                if (selectedAnswer == currentQuestion.CorrectAnswerIndex)
                {
                    quizScore++;
                    AppendToChatHistory($"Correct! {currentQuestion.Explanation}", Brushes.Green);
                }
                else
                {
                    AppendToChatHistory($"Incorrect. {currentQuestion.Explanation}", Brushes.Red);
                }
                LogActivity($"Answered quiz question {currentQuizQuestionIndex + 1}: {(selectedAnswer == currentQuestion.CorrectAnswerIndex ? "Correct" : "Incorrect")}");
                currentQuizQuestionIndex++;
                if (currentQuizQuestionIndex < quizQuestions.Count)
                {
                    DisplayQuizQuestion();
                }
                else
                {
                    isQuizMode = false;
                    string feedback = quizScore >= 8 ? "Great job! You're a cybersecurity pro!" : "Keep learning to stay safe online!";
                    AppendToChatHistory($"Quiz completed! Your score: {quizScore}/{quizQuestions.Count}. {feedback}", Brushes.Green);
                    LogActivity($"Quiz completed with score: {quizScore}/{quizQuestions.Count}");
                }
            }
            else
            {
                AppendToChatHistory("Please select a valid option (e.g., 'A' or '1'). Try again.", Brushes.Red);
            }
        }

        //logic for displaying quiz questions.
        private void DisplayQuizQuestion()
        {
            var question = quizQuestions[currentQuizQuestionIndex];
            AppendToChatHistory($"\nQuestion {currentQuizQuestionIndex + 1}: {question.Question}", Brushes.Green);
            for (int i = 0; i < question.Options.Length; i++)
            {
                AppendToChatHistory($"{(char)('A' + i)}) {question.Options[i]}", Brushes.Green);
            }
        }

        //logic for initializing quiz questions.
        private void InitializeQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Options = new[] { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Reporting phishing emails helps prevent scams."
                },
                new QuizQuestion
                {
                    Question = "Is it safe to use the same password for multiple accounts?",
                    Options = new[] { "Yes", "No" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Using the same password increases risk if one account is compromised."
                },
                new QuizQuestion
                {
                    Question = "What is a common sign of a phishing email?",
                    Options = new[] { "Personalized greeting", "Urgent language or threats", "Clear company logo", "No links included" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Phishing emails often use urgent language to trick users."
                },
                new QuizQuestion
                {
                    Question = "What does two-factor authentication (2FA) add to your account security?",
                    Options = new[] { "A second password", "A biometric scan only", "An additional verification step", "A new email address" },
                    CorrectAnswerIndex = 2,
                    Explanation = "2FA requires a second form of verification, like a code sent to your phone."
                },
                new QuizQuestion
                {
                    Question = "Should you share personal details on social media?",
                    Options = new[] { "Yes, freely", "Only with friends", "As little as possible", "Only on private profiles" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Limiting personal details reduces the risk of identity theft."
                },
                new QuizQuestion
                {
                    Question = "What is a key feature of a secure password?",
                    Options = new[] { "Your name", "Short and simple", "Mix of letters, numbers, and symbols", "Your birthday" },
                    CorrectAnswerIndex = 2,
                    Explanation = "A secure password includes a mix of characters to increase complexity."
                },
                new QuizQuestion
                {
                    Question = "What should you do with suspicious links in emails?",
                    Options = new[] { "Click to investigate", "Hover to check URL", "Avoid clicking", "Forward to friends" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Avoid clicking suspicious links to prevent malware or phishing attacks."
                },
                new QuizQuestion
                {
                    Question = "What is social engineering?",
                    Options = new[] { "Building social media apps", "Manipulating people to gain information", "Encrypting data", "Creating secure networks" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Social engineering involves tricking people into revealing sensitive information."
                },
                new QuizQuestion
                {
                    Question = "How often should you review app permissions?",
                    Options = new[] { "Never", "Once a year", "Regularly", "Only when installing" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Regularly reviewing app permissions helps maintain your privacy."
                },
                new QuizQuestion
                {
                    Question = "What is a common tactic used by scammers?",
                    Options = new[] { "Clear communication", "Offering unrealistic deals", "Using official company emails", "Providing full transparency" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Scammers often offer deals that seem too good to be true."
                }
            };
        }

        //logic for appending messages to chat history.
        private void AppendToChatHistory(string message, Brush color)
        {
            ChatHistoryText.Inlines.Add(new Run(message + "\n") { Foreground = color });
            ScrollViewer scrollViewer = ChatHistoryText.Parent as ScrollViewer;
            scrollViewer?.ScrollToBottom();
        }

        //logic for logging user activity.
        private void LogActivity(string action)
        {
            activityLog.Add($"{DateTime.Now:dd/MM/yyyy HH:mm}: {action}");
        }
    }
}
//References:
//Gillis, A., 2024. TechTarget. [Online] Available at: https://www.techtarget.com/searchsecurity/definition/phishing [Accessed 26 June 2025].
//Anon., 2025. TechTarget. [Online] Available at: https://www.techtarget.com/searchsecurity/definition/pharming [Accessed 20 June 2025].
//TechTarget, 2025. TechTarget. [Online] Available at: https://www.techtarget.com/searchsecurity/definition/password [Accessed 20 June 2025]
// Geeks for Geeks, 2025. Geeks for Geeks. [Online] Available at: https://www.geeksforgeeks.org/ascii-table/ [Accessed 14 April 2025].
//W3Schools, 2025. W3Schools. [Online] Available at: https://www.w3schools.com/cs/index.php [Accessed 14 April 2025]. 
////W3Schools, 2025. W3Schools. [Online] Available at: https://www.w3schools.com/cs/index.php [Accessed 24 May 2025].
