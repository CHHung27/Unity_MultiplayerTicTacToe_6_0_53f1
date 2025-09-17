using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using static CodeMonkey.CSharpCourse.Interactive.CodeMonkeyInteractiveSO;

namespace CodeMonkey.CSharpCourse.Interactive {

    public class MainWindow : EditorWindow {

        [SerializeField] private VisualTreeAsset visualTreeAsset;
        [SerializeField] private VisualTreeAsset lectureSingleVisualTreeAsset;
        [SerializeField] private VisualTreeAsset lectureHeaderVisualTreeAsset;
        [SerializeField] private VisualTreeAsset textTemplateVisualTreeAsset;
        [SerializeField] private VisualTreeAsset codeTemplateVisualTreeAsset;
        [SerializeField] private VisualTreeAsset videoTemplateVisualTreeAsset;


        private enum SubWindow {
            MainMenu,
            LectureList,
            Lecture,
        }

        private ScrollView lectureListScrollView;
        private VisualElement lectureListVisualElement;
        private VisualElement selectedLectureVisualElement;
        private VisualElement mainMenuVisualElement;
        private Vector2 lastLectureListScrollPosition;


        [MenuItem("Code Monkey/Code Monkey Main", priority = 0)]
        public static void ShowWindow() {
            MainWindow window = GetWindow<MainWindow>();
            window.titleContent = new GUIContent("Code Monkey Main");
        }

        public static void DestroyChildren(VisualElement containerVisualElement) {
            foreach (VisualElement child in containerVisualElement.Children().ToList()) {
                containerVisualElement.Remove(child);
            }
        }

        public static void AddComplexText(
            VisualTreeAsset textTemplateVisualTreeAsset,
            VisualTreeAsset codeTemplateVisualTreeAsset,
            VisualTreeAsset videoTemplateVisualTreeAsset,
            VisualElement containerVisualElement,
            string text) {
            // Break down complex text and add all components

            // ##REF##video_small, KGFAnwkO0Pk, What are Value Types and Reference Types in C#? (Class vs Struct)##REF##
            // ##REF##code, Console.WriteLine("Qwerty");##REF##

            // Parse HTML
            text = text.Replace("<h1>", "<size=20>");
            text = text.Replace("</h1>", "</size>");
            text = text.Replace("<strong>", "<b>");
            text = text.Replace("</strong>", "</b>");
            text = text.Replace("<p>", "<br>");
            text = text.Replace("</p>", "");

            string refTag = "##REF##";
            string textRemaining = text;
            int safety = 0;
            while (textRemaining.IndexOf(refTag) != -1 && safety < 100) {
                // Found Ref Tag
                int refTagIndex = textRemaining.IndexOf(refTag);

                // Add before text
                string textBefore = textRemaining.Substring(0, refTagIndex);
                AddText(textTemplateVisualTreeAsset, containerVisualElement, textBefore);

                string refData = textRemaining.Substring(refTagIndex + refTag.Length);
                refData = refData.Substring(0, refData.IndexOf(refTag));

                textRemaining = textRemaining.Substring(refTagIndex + refTag.Length);
                textRemaining = textRemaining.Substring(textRemaining.IndexOf(refTag) + refTag.Length);

                string[] refDataArray = refData.Split(',');
                string refType = refDataArray[0].Trim();
                switch (refType) {
                    case "video_small":
                        string youTubeId = refDataArray[1].Trim();
                        string youTubeTitle = refDataArray[2].Trim();
                        string thumbnailUrl = $"https://img.youtube.com/vi/{youTubeId}/mqdefault.jpg";
                        AddVideoReference(videoTemplateVisualTreeAsset, containerVisualElement, thumbnailUrl, youTubeTitle, "https://www.youtube.com/watch?v=" + youTubeId);
                        break;
                    case "code":
                        AddCode(codeTemplateVisualTreeAsset, containerVisualElement, refData.Substring(refType.Length + 1).Trim());
                        break;
                }
                safety++;
            }
            // No more Ref tags found
            AddText(textTemplateVisualTreeAsset, containerVisualElement, textRemaining);
        }

        public static void AddText(VisualTreeAsset textTemplateVisualTreeAsset, VisualElement containerVisualElement, string text) {
            VisualElement textVisualElement = textTemplateVisualTreeAsset.Instantiate();

            Label textLabel = textVisualElement.Q<Label>("textLabel");
            textLabel.text = text;

            containerVisualElement.Add(textVisualElement);
        }

        public static void AddCode(VisualTreeAsset codeTemplateVisualTreeAsset, VisualElement containerVisualElement, string codeString) {
            VisualElement codeVisualElement = codeTemplateVisualTreeAsset.Instantiate();

            Label textLabel = codeVisualElement.Q<Label>("codeLabel");
            textLabel.text = codeString;

            containerVisualElement.Add(codeVisualElement);
        }

        public static void AddVideoReference(VisualTreeAsset videoTemplateVisualTreeAsset, VisualElement containerVisualElement, string imageUrl, string title, string url, VideoReferenceSettings videoReferenceSettings = null) {
            Sprite waitingSprite = null;
            VisualElement videoVisualElement = AddVideoReference(videoTemplateVisualTreeAsset, containerVisualElement, waitingSprite, title, url, videoReferenceSettings);

            UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(imageUrl);
            unityWebRequest.SendWebRequest().completed += (AsyncOperation asyncOperation) => {
                try {
                    UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = asyncOperation as UnityWebRequestAsyncOperation;

                    if (unityWebRequestAsyncOperation.webRequest.result == UnityWebRequest.Result.ConnectionError ||
                        unityWebRequestAsyncOperation.webRequest.result == UnityWebRequest.Result.DataProcessingError ||
                        unityWebRequestAsyncOperation.webRequest.result == UnityWebRequest.Result.ProtocolError) {
                        // Error
                        //onError(unityWebRequest.error);
                    } else {
                        DownloadHandlerTexture downloadHandlerTexture = unityWebRequest.downloadHandler as DownloadHandlerTexture;
                        VisualElement imageVisualElement = videoVisualElement.Q<VisualElement>("image");
                        imageVisualElement.style.backgroundImage = new StyleBackground(downloadHandlerTexture.texture);
                    }
                } catch (Exception) {
                }
                unityWebRequest.Dispose();
            };
        }

        public static VisualElement AddVideoReference(VisualTreeAsset videoTemplateVisualTreeAsset, VisualElement containerVisualElement, Sprite sprite, string title, string url, VideoReferenceSettings videoReferenceSettings = null) {
            VisualElement videoVisualElement = videoTemplateVisualTreeAsset.Instantiate();

            VisualElement videoContainer = videoVisualElement.Q<VisualElement>("videoContainer");
            videoContainer.RegisterCallback<ClickEvent>((ClickEvent clickEvent) => {
                Debug.Log("Clicked: " + url);
                Application.OpenURL(url);
            });

            VisualElement imageVisualElement = videoContainer.Q<VisualElement>("image");
            imageVisualElement.style.backgroundImage = new StyleBackground(sprite);

            Label textLabel = videoContainer.Q<Label>("titleLabel");
            textLabel.text = title;

            if (videoReferenceSettings != null) {
                if (videoReferenceSettings.height != null) {
                    imageVisualElement.style.height = new StyleLength(videoReferenceSettings.height.Value);
                }
                if (videoReferenceSettings.fontSize != null) {
                    textLabel.style.fontSize = new StyleLength(videoReferenceSettings.fontSize.Value);
                }
            }

            containerVisualElement.Add(videoVisualElement);

            return videoVisualElement;
        }

        public class VideoReferenceSettings {
            public float? height;
            public float? fontSize;
        }

        private SubWindow GetActiveSubWindow() {
            if (lectureListVisualElement.style.display == DisplayStyle.Flex) {
                return SubWindow.LectureList;
            }
            if (selectedLectureVisualElement.style.display == DisplayStyle.Flex) {
                return SubWindow.Lecture;
            }
            return SubWindow.MainMenu;
        }

        public void OnDestroy() {
            CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO().OnStateChanged -= CodeMonkeyInteractiveSO_OnStateChanged;
        }

        private void CodeMonkeyInteractiveSO_OnStateChanged(object sender, EventArgs e) {
            switch (GetActiveSubWindow()) {
                case SubWindow.MainMenu:
                    ShowMainMenu();
                    break;
                case SubWindow.LectureList:
                    ShowLectureButtons();
                    break;
                case SubWindow.Lecture:
                    ShowLecture();
                    break;
            }
        }

        public void CreateGUI() {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO().OnStateChanged -= CodeMonkeyInteractiveSO_OnStateChanged;
            CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO().OnStateChanged += CodeMonkeyInteractiveSO_OnStateChanged;

            // Instantiate UXML
            VisualElement rootVisualTreeAsset = visualTreeAsset.Instantiate();
            rootVisualTreeAsset.style.flexGrow = 1f;
            root.Add(rootVisualTreeAsset);

            lectureListScrollView = root.Q<ScrollView>("lectureListScrollView");
            lectureListVisualElement = root.Q<VisualElement>("lectureList");
            selectedLectureVisualElement = root.Q<VisualElement>("selectedLectureContainer");
            mainMenuVisualElement = root.Q<VisualElement>("mainMenu");

            root.Q<Label>("versionLabel").text = CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO().currentVersion;

            Button lectureListButton = mainMenuVisualElement.Q<Button>("lectureListButton");
            lectureListButton.RegisterCallback((ClickEvent clickEvent) => {
                TESTING_OnLectureListButtonClick();
                ShowLectureButtons();
            });

            Button backToMainMenuButton = lectureListVisualElement.Q<Button>("backToMainMenuButton");
            backToMainMenuButton.RegisterCallback((ClickEvent clickEvent) => {
                ShowMainMenu();
            });


            ObjectField objectField = rootVisualElement.Q<ObjectField>("scriptableObjectField");
            objectField.RegisterValueChangedCallback((ChangeEvent<UnityEngine.Object> evt) => {
                ShowLecture();
            });

            Button backToLectureListButton = selectedLectureVisualElement.Q<Button>("backButton");
            backToLectureListButton.RegisterCallback((ClickEvent clickEvent) => {
                ObjectField objectField = rootVisualElement.Q<ObjectField>("scriptableObjectField");
                objectField.value = null;
                ShowLectureButtons();
            });

            lectureListScrollView.RegisterCallback((GeometryChangedEvent evt) => {
                lectureListScrollView.scrollOffset = lastLectureListScrollPosition;
            });

            ShowMainMenu();
        }

        private void ShowMainMenu() {
            lectureListVisualElement.style.display = DisplayStyle.None;
            selectedLectureVisualElement.style.display = DisplayStyle.None;
            mainMenuVisualElement.style.display = DisplayStyle.Flex;

            // Check for updates
            CodeMonkeyInteractiveSO.CheckForUpdates((string currentVersion, string newVersion) => {
                if (currentVersion == newVersion) {
                    mainMenuVisualElement.Q<VisualElement>("checkingForUpdates").style.display = DisplayStyle.None;
                    return;
                }

                VisualElement checkingForUpdatesVisualElement =
                    mainMenuVisualElement.Q<VisualElement>("checkingForUpdates");
                checkingForUpdatesVisualElement.style.display = DisplayStyle.Flex;
                Label textLabel = checkingForUpdatesVisualElement.Q<Label>();
                textLabel.text = "New version available!\n" +
                    currentVersion + " -> " + newVersion + "\n" +
                    "<u>Click here!</u>";

                textLabel.RegisterCallback((ClickEvent clickEvent) => {
                    string url = "https://unitycodemonkey.teachable.com/courses/learn-how-to-make-the-simplest-multiplayer-game/lectures/59314108";
                    Application.OpenURL(url);
                });
            });


            // Totals
            VisualElement totalsVisualElement =
                mainMenuVisualElement.Q<VisualElement>("totals");

            Label totalsLabel = totalsVisualElement.Q<Label>("totalsLabel");

            CodeMonkeyInteractiveSO.GetCompletionStats(
                out int faqCompleted, out int faqTotals,
                out int quizCompleted, out int quizTotals,
                out int exercisesCompleted, out int exercisesTotals);

            totalsLabel.text = $"FAQ: {faqCompleted} / {faqTotals}\n" +
                $"Quiz: {quizCompleted} / {quizTotals}\n";
                //$"Exercises: {exercisesCompleted} / {exercisesTotals}";


            // Message
            VisualElement messageVisualElement =
                mainMenuVisualElement.Q<VisualElement>("message");

            CodeMonkeyInteractiveSO.GetLatestMessage((WebsiteLatestMessage websiteLatestMessage) => {
                messageVisualElement.Q<Label>("messageLabel").text = websiteLatestMessage.text;
            });



            // QOTD
            VisualElement qotdVisualElement =
                mainMenuVisualElement.Q<VisualElement>("qotd");

            Action openQotdURL = () => {
                string qotdUrl = "https://unitycodemonkey.com/qotd_ask.php?q=30";
                Application.OpenURL(qotdUrl);
            };
            qotdVisualElement.RegisterCallback((ClickEvent clickEvent) => {
                openQotdURL();
            });

            qotdVisualElement.Q<Label>("questionLabel").text = "...";
            qotdVisualElement.Q<Button>("answerAButton").style.display = DisplayStyle.None;
            qotdVisualElement.Q<Button>("answerBButton").style.display = DisplayStyle.None;
            qotdVisualElement.Q<Button>("answerCButton").style.display = DisplayStyle.None;
            qotdVisualElement.Q<Button>("answerDButton").style.display = DisplayStyle.None;
            qotdVisualElement.Q<Button>("answerEButton").style.display = DisplayStyle.None;

            CodeMonkeyInteractiveSO.GetLastQOTD((CodeMonkeyInteractiveSO.LastQOTDResponse lastQOTDResponse) => {
                openQotdURL = () => {
                    string qotdUrl = "https://unitycodemonkey.com/qotd_ask.php?q=" + lastQOTDResponse.questionId;
                    Application.OpenURL(qotdUrl);
                };

                qotdVisualElement.Q<Label>("questionLabel").text = lastQOTDResponse.questionText;
                if (!string.IsNullOrEmpty(lastQOTDResponse.answerA)) {
                    qotdVisualElement.Q<Button>("answerAButton").style.display = DisplayStyle.Flex;
                    qotdVisualElement.Q<Button>("answerAButton").text = lastQOTDResponse.answerA;
                }
                if (!string.IsNullOrEmpty(lastQOTDResponse.answerB)) {
                    qotdVisualElement.Q<Button>("answerBButton").style.display = DisplayStyle.Flex;
                    qotdVisualElement.Q<Button>("answerBButton").text = lastQOTDResponse.answerB;
                }
                if (!string.IsNullOrEmpty(lastQOTDResponse.answerC)) {
                    qotdVisualElement.Q<Button>("answerCButton").style.display = DisplayStyle.Flex;
                    qotdVisualElement.Q<Button>("answerCButton").text = lastQOTDResponse.answerC;
                }
                if (!string.IsNullOrEmpty(lastQOTDResponse.answerD)) {
                    qotdVisualElement.Q<Button>("answerDButton").style.display = DisplayStyle.Flex;
                    qotdVisualElement.Q<Button>("answerDButton").text = lastQOTDResponse.answerD;
                }
                if (!string.IsNullOrEmpty(lastQOTDResponse.answerE)) {
                    qotdVisualElement.Q<Button>("answerEButton").style.display = DisplayStyle.Flex;
                    qotdVisualElement.Q<Button>("answerEButton").text = lastQOTDResponse.answerE;
                }
            });


            // Dynamic Message
            VisualElement dynamicMessageVisualElement =
                mainMenuVisualElement.Q<VisualElement>("dynamicMessage");

            Func<string> getDynamicMessageURL = () => "https://unitycodemonkey.com/";
            dynamicMessageVisualElement.RegisterCallback((ClickEvent clickEvent) => {
                Application.OpenURL(getDynamicMessageURL());
            });

            CodeMonkeyInteractiveSO.GetWebsiteDynamicMessage((WebsiteDynamicMessage websiteDynamicMessage) => {
                dynamicMessageVisualElement.Q<Label>("messageLabel").text = websiteDynamicMessage.text;
                getDynamicMessageURL = () => websiteDynamicMessage.url;
            });


            // Latest Videos
            VisualElement latestVideosVisualElement =
                mainMenuVisualElement.Q<VisualElement>("latestVideos");

            latestVideosVisualElement.Q<VisualElement>("_1Container").Clear();
            latestVideosVisualElement.Q<VisualElement>("_2Container").Clear();
            latestVideosVisualElement.Q<VisualElement>("_3Container").Clear();
            latestVideosVisualElement.Q<VisualElement>("_4Container").Clear();



            CodeMonkeyInteractiveSO.GetWebsiteLatestVideos((LatestVideos latestVideos) => {
                AddLatestVideoReference(latestVideos.videos[0], latestVideosVisualElement.Q<VisualElement>("_1Container"));
                AddLatestVideoReference(latestVideos.videos[1], latestVideosVisualElement.Q<VisualElement>("_2Container"));
                AddLatestVideoReference(latestVideos.videos[2], latestVideosVisualElement.Q<VisualElement>("_3Container"));
                AddLatestVideoReference(latestVideos.videos[3], latestVideosVisualElement.Q<VisualElement>("_4Container"));
            });

            void AddLatestVideoReference(LatestVideoSingle latestVideoSingle, VisualElement containerVisualElement) {
                string thumbnailUrl = $"https://img.youtube.com/vi/{latestVideoSingle.youTubeId}/mqdefault.jpg";
                string url = $"https://unitycodemonkey.com/video.php?v={latestVideoSingle.youTubeId}";
                AddVideoReference(
                    videoTemplateVisualTreeAsset,
                    containerVisualElement,
                    thumbnailUrl,
                    latestVideoSingle.title,
                    url,
                    new VideoReferenceSettings {
                        height = 80,
                        fontSize = 9,
                    }
                );
            }

            if (CodeMonkeyInteractiveSO.TryLoad()) {
                ShowMainMenu();
            }
        }

        private void PrintAllTitles() {
            CodeMonkeyInteractiveSO codeMonkeyInteractiveSO = CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO();
            foreach (LectureSO lectureSO in codeMonkeyInteractiveSO.lectureListSO.lectureSOList) {
                Debug.Log("Lecture " + lectureSO.lectureCode);
                string faq = "";
                string quiz = "";
                foreach (FrequentlyAskedQuestionSO frequentlyAskedQuestionSO in lectureSO.frequentlyAskedQuestionListSO.frequentlyAskedQuestionSOList) {
                    faq += frequentlyAskedQuestionSO.title + "\n\n";
                }
                foreach (QuizSO quizSO in lectureSO.quizListSO.quizSOList) {
                    quiz += quizSO.question + "\n\n";
                }
                Debug.Log(faq);
                Debug.Log(quiz);
                Debug.Log("------------");
            }
        }

        private void WriteFileFAQData() {
            string text = "";
            CodeMonkeyInteractiveSO codeMonkeyInteractiveSO = CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO();
            foreach (LectureSO lectureSO in codeMonkeyInteractiveSO.lectureListSO.lectureSOList) {
                text += "####### Lecture " + lectureSO.lectureTitle + "\n\n";
                string faq = "";

                foreach (FrequentlyAskedQuestionSO frequentlyAskedQuestionSO in lectureSO.frequentlyAskedQuestionListSO.frequentlyAskedQuestionSOList) {
                    faq += "FAQ: " + frequentlyAskedQuestionSO.title + "\n";
                    faq += frequentlyAskedQuestionSO.text + "\n\n\n";
                }
                text += "#### FREQUENTLY ASKED QUESTIONS" + "\n";
                text += faq;

                text += "------------\n\n\n\n";
            }
            File.WriteAllText(Application.dataPath + "/courseFAQ.txt", text);
        }

        private void WriteFileQuizData() {
            string text = "";
            CodeMonkeyInteractiveSO codeMonkeyInteractiveSO = CodeMonkeyInteractiveSO.GetCodeMonkeyInteractiveSO();
            foreach (LectureSO lectureSO in codeMonkeyInteractiveSO.lectureListSO.lectureSOList) {
                text += "####### Lecture " + lectureSO.lectureTitle + "\n\n";
                string quiz = "";

                foreach (QuizSO quizSO in lectureSO.quizListSO.quizSOList) {
                    quiz += "Question: " + quizSO.question+ "\n";
                    for (int i = 0; i < quizSO.optionList.Count; i++) {
                        quiz += i + ") " + quizSO.optionList[i] + "\n";
                    }
                    quiz += "\n";
                    quiz += "Correct: " + quizSO.correctOptionIndex + "\n\n";
                    quiz += quizSO.answer + "\n\n\n";
                }
                text += "#### QUIZ" + "\n";
                text += quiz;

                text += "------------\n\n\n\n";
            }
            File.WriteAllText(Application.dataPath + "/courseQuiz.txt", text);
        }

        private void ShowLectureButtons() {
            lectureListVisualElement.style.display = DisplayStyle.Flex;
            selectedLectureVisualElement.style.display = DisplayStyle.None;
            mainMenuVisualElement.style.display = DisplayStyle.None;

            // Remove old questions
            MainWindow.DestroyChildren(lectureListScrollView);

            // Spawn Lectures
            LectureSO lastLectureSO = null;
            foreach (LectureSO lectureSO in LectureListSO.GetLectureListSO().lectureSOList) {
                if (lastLectureSO == null) {
                    // First lecture
                    VisualElement lectureHeader = lectureHeaderVisualTreeAsset.Instantiate();
                    lectureListScrollView.Add(lectureHeader);
                    lectureHeader.Q<Label>().text = "LECTURE LIST";
                }
                /*
                if (lectureSO.lectureCode == 1010) {
                    VisualElement lectureHeader = lectureHeaderVisualTreeAsset.Instantiate();
                    lectureListScrollView.Add(lectureHeader);
                    lectureHeader.Q<Label>().text = "-";
                }
                */

                VisualElement lectureSingle = lectureSingleVisualTreeAsset.Instantiate();
                lectureSingle.name = "lectureSingle_" + lectureSO.name;

                lectureSO.GetLectureStats(out int faqDone,
                    out int faqTotal,
                    out int quizDone,
                    out int quizTotal,
                    out int exercisesDone,
                    out int exercisesTotal);

                Label completionStatsLabel = lectureSingle.Q<Label>("completionStatsLabel");
                completionStatsLabel.text =
                    (faqDone + quizDone + exercisesDone) + " / " +
                    (faqTotal + quizTotal + exercisesTotal);

                bool allDone =
                    (faqDone + quizDone + exercisesDone) >=
                    (faqTotal + quizTotal + exercisesTotal);

                if (allDone) {
                    completionStatsLabel.text = $"<color=#00ff00>{completionStatsLabel.text}</color>";
                }

                lectureSingle.Q<Button>("button").text = lectureSO.lectureSectionNumber + ". " + lectureSO.lectureTitle;
                lectureSingle.RegisterCallback<ClickEvent>((ClickEvent clickEvent) => {
                    lastLectureListScrollPosition = lectureListScrollView.scrollOffset;
                    ObjectField objectField = rootVisualElement.Q<ObjectField>("scriptableObjectField");
                    objectField.value = lectureSO;
                });

                lectureListScrollView.Add(lectureSingle);

                lastLectureSO = lectureSO;
            }
            lectureListScrollView.schedule.Execute(() => {
                lectureListScrollView.scrollOffset = lastLectureListScrollPosition;
            });
            lectureListScrollView.scrollOffset = lastLectureListScrollPosition;
        }

        private void ShowLecture() {
            ObjectField objectField = rootVisualElement.Q<ObjectField>("scriptableObjectField");
            if (objectField.value != null) {
                LectureSO lectureSO = objectField.value as LectureSO;
                ShowLecture(lectureSO);
            }
        }

        private void ShowLecture(LectureSO lectureSO) {
            CodeMonkeyInteractiveSO.SetLastSelectedLectureSO(lectureSO);

            lectureListVisualElement.style.display = DisplayStyle.None;
            selectedLectureVisualElement.style.display = DisplayStyle.Flex;
            mainMenuVisualElement.style.display = DisplayStyle.None;

            Button frequentlyAskedQuestionButton = selectedLectureVisualElement.Q<Button>("frequentlyAskedQuestionButton");
            Button quizButton = selectedLectureVisualElement.Q<Button>("quizButton");
            Button exercisesButton = selectedLectureVisualElement.Q<Button>("exercisesButton");

            LectureSO.LectureStats lectureStats = lectureSO.GetLectureStats();

            frequentlyAskedQuestionButton.text = $"FAQ ({lectureStats.faqDone}/{lectureStats.faqTotal})";
            quizButton.text = $"Quiz ({lectureStats.quizDone}/{lectureStats.quizTotal})";
            exercisesButton.text = $"Exercises ({lectureStats.exercisesDone}/{lectureStats.exercisesTotal})";
            exercisesButton.visible = false;

            frequentlyAskedQuestionButton.RegisterCallback((ClickEvent clickEvent) => {
                GetWindow<FrequentlyAskedQuestionsWindow>().SetLectureSO(lectureSO);
            });
            quizButton.RegisterCallback((ClickEvent clickEvent) => {
                GetWindow<QuizWindow>().SetLectureSO(lectureSO);
            });
            exercisesButton.RegisterCallback((ClickEvent clickEvent) => {
                GetWindow<ExercisesWindow>().SetLectureSO(lectureSO);
            });

            VisualElement textContainerVisualElement = selectedLectureVisualElement.Q<VisualElement>("textContainerVisualElement");
            MainWindow.DestroyChildren(textContainerVisualElement);
            MainWindow.AddComplexText(
                textTemplateVisualTreeAsset,
                codeTemplateVisualTreeAsset,
                videoTemplateVisualTreeAsset,
                textContainerVisualElement,
                lectureSO.lectureDescription
            );
        }



        private void ParseFAQJson() {
            /* ChatGPT Prompt:
                    I will give you some questions and you should convert them into separate json objects (one for each question) 
                    where it has fields for "question" for the question, 
                    and "answer" for the answer
            */
            string jsonArray = "[\r\n  {\r\n    \"question\": \"What is the purpose of the polish stage in game development?\",\r\n    \"answer\": \"The polish stage is all about enhancing the game's visual, audio, and overall user experience. It’s what elevates a game from great to AWESOME!\"\r\n  },\r\n  {\r\n    \"question\": \"How were animations added to objects like the cross and circle?\",\r\n    \"answer\": \"The FEEL asset was used to add effects like squash and stretch. All in 1 Sprite Shader was used for extra visual effects.\"\r\n  },\r\n  {\r\n    \"question\": \"What post-processing effects were applied to the game?\",\r\n    \"answer\": \"Simple bloom to make objects glow and a vignette to darken the corners of the screen were added for subtle but effective visual enhancements.\"\r\n  },\r\n  {\r\n    \"question\": \"How was the 'connecting' UI implemented?\",\r\n    \"answer\": \"A simple static message (animated using Text Animator), and it listens to an event OnGameStarted to hide itself.\"\r\n  },\r\n  {\r\n    \"question\": \"Why is it important to add particle effects and animations during the polish stage?\",\r\n    \"answer\": \"Particle effects and animations bring the game to life, making actions like placing objects or winning more visually satisfying and engaging.\"\r\n  },\r\n  {\r\n    \"question\": \"Can I use these polish techniques in other types of games?\",\r\n    \"answer\": \"Absolutely! The techniques and tools used here, like FEEL, Text Animator, and post-processing effects, are versatile and can be applied to various game genres to enhance their visual and audio appeal.\"\r\n  }\r\n]\r\n";
            string json = "{\"array\":" + jsonArray + "}";
            Debug.Log(json);
            FAQJson rootobject = JsonUtility.FromJson<FAQJson>(json);

            string lectureCode = "1190";
            int index = 1;

            FrequentlyAskedQuestionListSO faqListSO = ScriptableObject.CreateInstance<FrequentlyAskedQuestionListSO>();
            faqListSO.frequentlyAskedQuestionSOList = new List<FrequentlyAskedQuestionSO>();

            foreach (FAQJsonSingle class1 in rootobject.array) {
                FrequentlyAskedQuestionSO faqSO = ScriptableObject.CreateInstance<FrequentlyAskedQuestionSO>();
                faqSO.title = class1.question;
                faqSO.text = class1.answer;

                faqListSO.frequentlyAskedQuestionSOList.Add(faqSO);

                string path = $"Assets/TicTacToeCompanion/CodeMonkeyInteractive/ScriptableObjects/FrequentlyAskedQuestionSO/L{lectureCode}/FAQ_L{lectureCode}_0{index}.asset";
                AssetDatabase.CreateAsset(faqSO, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = faqSO;

                index++;
            }

            {
                string path = $"Assets/TicTacToeCompanion/CodeMonkeyInteractive/ScriptableObjects/FrequentlyAskedQuestionSO/L{lectureCode}/_FAQList_L{lectureCode}.asset";
                AssetDatabase.CreateAsset(faqListSO, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [Serializable]
        public class FAQJson {
            public FAQJsonSingle[] array;
        }

        [Serializable]
        public class FAQJsonSingle {
            public string question;
            public string answer;
        }






        private void ParseQuizJson() {
            /* ChatGPT Prompt:
                    I will give you some questions and you should convert them into separate json objects (one for each question) 
                    where it has fields for "question" for the question, 
                    "option0", "option1", "option2", "option3" (each being one line in the question) 
                    then a field for "correct" which should be the index for the correct option (0, 1, 2 or 3) 
                    and finally an "answer" field that contains the explanation
                    For the options make them based on the line order instead of a-b-c-d, 
                    the first line should always be option0, regardless of what letter it uses
            */
            string jsonArray = "[\r\n  {\r\n    \"question\": \"Why is the polish stage important in game development?\",\r\n    \"option0\": \"It fixes all major bugs in the game\",\r\n    \"option1\": \"It makes the game look and feel more professional and enjoyable\",\r\n    \"option2\": \"It simplifies the codebase\",\r\n    \"option3\": \"It optimizes performance\",\r\n    \"correct\": 1,\r\n    \"answer\": \"The polish stage is meant to enhance the game as a whole, mainly focusing on visuals, animations, sounds, but of course also important to improve the UI, UX, Bugfixing, etc. This is the very important stage that can take your game from great to AWESOME!\"\r\n  },\r\n  {\r\n    \"question\": \"What asset did I use to add animations and effects like squash and stretch to objects?\",\r\n    \"option0\": \"Text Animator\",\r\n    \"option1\": \"All-in-One Sprite Shader\",\r\n    \"option2\": \"FEEL\",\r\n    \"option3\": \"Unity Post-Processing\",\r\n    \"correct\": 2,\r\n    \"answer\": \"The FEEL asset was used to add animations like squash and stretch to enhance the visual effects. It's an excellent asset, I highly recommend it.\"\r\n  },\r\n  {\r\n    \"question\": \"What tool was used to add outlines and blur effects to sprites?\",\r\n    \"option0\": \"Text Animator\",\r\n    \"option1\": \"All-in-One Sprite Shader\",\r\n    \"option2\": \"Unity Visual Effects Graph\",\r\n    \"option3\": \"FEEL\",\r\n    \"correct\": 1,\r\n    \"answer\": \"The All-in-One Sprite Shader was used to add outlines and blur effects to enhance sprite visuals. It's another excellent asset, I highly recommend it.\"\r\n  },\r\n  {\r\n    \"question\": \"What visual effect was added to the game’s background for extra polish?\",\r\n    \"option0\": \"Static image\",\r\n    \"option1\": \"Gradient overlay\",\r\n    \"option2\": \"Dynamic lighting\",\r\n    \"option3\": \"Particle effects\",\r\n    \"correct\": 3,\r\n    \"answer\": \"I added a simple particle effect to the background to make the game look more dynamic. Simple but effective.\"\r\n  },\r\n  {\r\n    \"question\": \"Which of the following tools was used to animate the 'YOU WIN' text?\",\r\n    \"option0\": \"FEEL\",\r\n    \"option1\": \"Text Animator\",\r\n    \"option2\": \"All-in-One Sprite Shader\",\r\n    \"option3\": \"Unity Animation Clips\",\r\n    \"correct\": 1,\r\n    \"answer\": \"The Text Animator asset was used to animate the 'YOU WIN' text with effects like wiggle. Another great polish tool, super easy to use and a tiny animation in your text makes all the difference in the world.\"\r\n  },\r\n  {\r\n    \"question\": \"What purpose do post-processing effects serve in the game?\",\r\n    \"option0\": \"They optimize performance\",\r\n    \"option1\": \"They add visual enhancements like bloom and vignette\",\r\n    \"option2\": \"They synchronize game objects\",\r\n    \"option3\": \"They manage player input\",\r\n    \"correct\": 1,\r\n    \"answer\": \"Post-processing effects, such as bloom and vignette, enhance the overall visual quality of the game.\"\r\n  },\r\n  {\r\n    \"question\": \"What was the purpose of the 'connecting' UI element?\",\r\n    \"option0\": \"To display player scores\",\r\n    \"option1\": \"To show a message while establishing the multiplayer connection\",\r\n    \"option2\": \"To allow players to chat\",\r\n    \"option3\": \"To display game instructions\",\r\n    \"correct\": 1,\r\n    \"answer\": \"The Connecting UI is a simple way to provide feedback to the player. It's always important that the players knows the game didn't freeze or crash.\"\r\n  },\r\n  {\r\n    \"question\": \"What is the main benefit of using third-party tools like Feel and Text Animator?\",\r\n    \"option0\": \"They improve code performance\",\r\n    \"option1\": \"They make it easier to add professional-quality effects quickly\",\r\n    \"option2\": \"They reduce the game's file size\",\r\n    \"option3\": \"They simplify multiplayer networking\",\r\n    \"correct\": 1,\r\n    \"answer\": \"Third-party tools like FEEL and Text Animator simplify the process of adding professional-quality effects, saving time (money) and effort.\"\r\n  }\r\n]\r\n";
            string json = "{\"array\":" + jsonArray + "}";
            Debug.Log(json);
            QuizJson rootobject = JsonUtility.FromJson<QuizJson>(json);

            string lectureCode = "1190";
            int index = 1;

            QuizListSO quizListSO = ScriptableObject.CreateInstance<QuizListSO>();
            quizListSO.quizSOList = new List<QuizSO>();

            foreach (QuizJsonSingle class1 in rootobject.array) {
                QuizSO quizSO = ScriptableObject.CreateInstance<QuizSO>();
                quizSO.question = class1.question;
                quizSO.optionList = new System.Collections.Generic.List<string> {
                    class1.option0,
                    class1.option1,
                    class1.option2,
                    class1.option3
                };
                quizSO.correctOptionIndex = class1.correct;
                quizSO.answer = class1.answer;

                quizListSO.quizSOList.Add(quizSO);

                string path = $"Assets/TicTacToeCompanion/CodeMonkeyInteractive/ScriptableObjects/QuizSO/L{lectureCode}/Quiz_L{lectureCode}_0{index}.asset";
                AssetDatabase.CreateAsset(quizSO, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = quizSO;

                index++;
            }

            {
                string path = $"Assets/TicTacToeCompanion/CodeMonkeyInteractive/ScriptableObjects/QuizSO/L{lectureCode}/_QuizList_L{lectureCode}.asset";
                AssetDatabase.CreateAsset(quizListSO, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }


        [Serializable]
        public class QuizJson {
            public QuizJsonSingle[] array;
        }

        [Serializable]
        public class QuizJsonSingle {
            public string question;
            public string option0;
            public string option1;
            public string option2;
            public string option3;
            public int correct;
            public string answer;
        }





        private void TESTING_OnLectureListButtonClick() {
            //Debug.Log("Lecture List Button");

            //PrintAllTitles();
            //WriteFileFAQData();
            //WriteFileQuizData();

            //ParseFAQJson();
            //ParseQuizJson();
        }


    }





}