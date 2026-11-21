using Microsoft.EntityFrameworkCore;
using CapstoneProjectPUSH_API.Models;
using CapstoneProjectPUSH_API.Services;

namespace CapstoneProjectPUSH_API.Data
{
    public static class DbSeeder
    {
        public const string SeedAdminDisplayName = "Administrator";
        public const string SeedAdminEmail = "admin@push.local";

        public static async Task SeedAsync(PushDbContext db, IConfiguration configuration)
        {
            // Order specific
            await SeedCategoriesAsync(db);
            await SeedGymsAsync(db);
            await SeedAdminAsync(db, configuration);
            await SeedGuestAsync(db);
            await SeedTrainersAsync(db);
            await SeedExercisesAsync(db);
            await SeedWorkoutsAsync(db);
            await SeedWorkoutExercisesAsync(db);
            await SeedNotesAsync(db);
            await SeedWorkoutLogsAsync(db);
            await SeedWeightLogsAsync(db);
            await SeedArticlesAsync(db);
        }

        // Helpers
        // Seed times are written as NZ local times, then stored as UTC
        private static DateTime SeedDate(int daysAgo, int hour)
        {
            TimeZoneInfo nzTimeZone;

            try
            {
                nzTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
            }
            catch (Exception)
            {
                nzTimeZone = TimeZoneInfo.Utc;
            }

            DateTime nzToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nzTimeZone).Date;
            DateTime nzLocal = DateTime.SpecifyKind(nzToday.AddDays(-daysAgo).AddHours(hour), DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(nzLocal, nzTimeZone);
        }

        // Looks up IDs by title so seeding doesn't depend on identity values starting at 1
        private static async Task<Dictionary<string, int>> GetCategoryIdsAsync(PushDbContext db)
        {
            Dictionary<string, int> ids = new Dictionary<string, int>();

            foreach (Category category in await db.Categories.ToListAsync())
            {
                ids[category.Title] = category.Id;
            }

            return ids;
        }

        private static int GetId(Dictionary<string, int> ids, string key)
        {
            int id;

            if (ids.TryGetValue(key, out id))
            {
                return id;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Seed lookup failed: " + key);
                return 0;
            }
        }

        // Seed guest
        private static async Task SeedGuestAsync(PushDbContext db)
        {
            // Database check
            bool guestExists = await db.Users.AnyAsync(u => u.IsGuest);

            if (guestExists)
            {
                return;
            }

            bool nameTaken = await db.Users.AnyAsync(u => u.DisplayName == "Guest");
            bool emailTaken = await db.Users.AnyAsync(u => u.Email == "guest@push.local");

            if (nameTaken || emailTaken)
            {
                return;
            }

            // Add guest
            db.Users.Add(new User
            {
                Email = "guest@push.local",
                DisplayName = "Guest",
                PasswordHash = string.Empty,
                IsGuest = true
            });

            await db.SaveChangesAsync();
        }

        // Seed admin account -- details stored in appsettings or user secrets
        private static async Task SeedAdminAsync(PushDbContext db, IConfiguration configuration)
        {
            string? adminPassword = configuration["SeedAdmin:Password"];

            // Validation
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                System.Diagnostics.Debug.WriteLine("Seed skipped: admin (SeedAdmin:Password not set)");
                return;
            }

            // Database check
            bool adminExists = await db.Users.AnyAsync(u => u.IsAdmin);

            if (adminExists)
            {
                return;
            }

            bool nameTaken = await db.Users.AnyAsync(u => u.DisplayName == SeedAdminDisplayName);
            bool emailTaken = await db.Users.AnyAsync(u => u.Email == SeedAdminEmail);

            if (nameTaken || emailTaken)
            {
                return;
            }

            // Add admin
            db.Users.Add(new User
            {
                Email = SeedAdminEmail,
                DisplayName = SeedAdminDisplayName,
                PasswordHash = PasswordHasher.HashPassword(adminPassword),
                IsAdmin = true
            });

            await db.SaveChangesAsync();
        }

        // Seed categories
        private static async Task SeedCategoriesAsync(PushDbContext db)
        {
            if (await db.Categories.AnyAsync())
            {
                return;
            }

            string[] categories = new string[]
            {
                "Strength Training",
                "Cardio",
                "HIIT",
                "Yoga",
                "Flexibility & Mobility",
                "Core",
                "Bodyweight",
                "Pilates",
                "Recovery"
            };

            foreach (string title in categories)
            {
                db.Categories.Add(new Category { Title = title });
            }

            await db.SaveChangesAsync();
        }

        // Seed gyms
        private static async Task SeedGymsAsync(PushDbContext db)
        {
            if (await db.Gyms.AnyAsync())
            {
                return;
            }

            List<Gym> gyms = new List<Gym>
            {
                // Auckland
                new Gym { Name = "Iron Fitness", Address = "14 Kauri Lane, Epsom", City = "Auckland" },
                new Gym { Name = "Harbour Strength Co.", Address = "89 Wharf Street, Newmarket", City = "Auckland" },
                new Gym { Name = "Summit Lift Club", Address = "5 Totara Avenue, Eden Terrace", City = "Auckland" },

                // Hamilton
                new Gym { Name = "Iron Fitness", Address = "210 River Road, Hamilton Central", City = "Hamilton" },

                // Wellington
                new Gym { Name = "Peak Performance", Address = "41 Rimu Street, Te Aro", City = "Wellington" },
                new Gym { Name = "Foundry Gym", Address = "12 Harbour View Road, Te Aro", City = "Wellington" },
                new Gym { Name = "Iron Fitness", Address = "73 Southern Cross Lane, Te Aro", City = "Wellington" },

                // Christchurch
                new Gym { Name = "Peak Performance", Address = "9 Matai Crescent, Belfast", City = "Christchurch" },
                new Gym { Name = "Southern Core Studio", Address = "150 Plains Road, Addington", City = "Christchurch" }
            };

            db.Gyms.AddRange(gyms);
            await db.SaveChangesAsync();
        }

        // Seed trainers
        private static async Task SeedTrainersAsync(PushDbContext db)
        {
            if (await db.Trainers.AnyAsync())
            {
                return;
            }

            Dictionary<string, int> categoryIds = await GetCategoryIdsAsync(db);

            // Gyms looked up by name, city
            Dictionary<string, int> gymIds = new Dictionary<string, int>();

            foreach (Gym gym in await db.Gyms.ToListAsync())
            {
                gymIds[gym.Name + "|" + gym.City] = gym.Id;
            }

            List<Trainer> trainers = new List<Trainer>
            {
                new Trainer
                {
                    FirstName = "Liam",
                    LastName = "Walker",
                    Email = "liam.walker@example.com",
                    PhoneNumber = "123-456-7890",
                    CategoryId = GetId(categoryIds, "Strength Training"),
                    Description = "Certified personal trainer with 8 years of experience in strength training. Liam focuses on proper technique and progressive overload, helping clients build lasting strength safely, whether they're picking up a barbell for the first time or working toward a new personal best.",
                    Available = true,
                    GymId = GetId(gymIds, "Iron Fitness|Auckland")
                },

                new Trainer
                {
                    FirstName = "Aroha",
                    LastName = "Ngata",
                    Email = "aroha.ngata@example.com",
                    PhoneNumber = "123-456-7890",
                    CategoryId = GetId(categoryIds, "HIIT"),
                    Description = "Certified personal trainer with 5 years of experience specialising in high-intensity interval training. Aroha designs fast-paced, varied sessions that build endurance and burn calories efficiently, and scales every workout to suit beginners through to experienced athletes.",
                    Available = false,
                    GymId = GetId(gymIds, "Iron Fitness|Hamilton")
                },

                new Trainer
                {
                    FirstName = "Sam",
                    LastName = "Patel",
                    Email = string.Empty,
                    PhoneNumber = "123-456-7890",
                    CategoryId = GetId(categoryIds, "Cardio"),
                    Description = "Cardio and endurance coach with 3 years of experience. Sam helps clients improve their heart health and stamina through running, rowing and cycling plans, building up gradually so progress lasts.",
                    Available = true,
                    GymId = GetId(gymIds, "Foundry Gym|Wellington")
                },

                new Trainer
                {
                    FirstName = "Mei",
                    LastName = "Chen",
                    Email = "mei.chen@example.com",
                    PhoneNumber = string.Empty,
                    CategoryId = GetId(categoryIds, "Yoga"),
                    Description = "Registered yoga instructor with 8 years of teaching experience. Mei runs calm, beginner-friendly classes that build flexibility, balance and breathing control, with options to progress for more experienced students.",
                    Available = true,
                    GymId = GetId(gymIds, "Peak Performance|Christchurch")
                }
            };

            db.Trainers.AddRange(trainers);
            await db.SaveChangesAsync();
        }

        // Seed exercises
        private static async Task SeedExercisesAsync(PushDbContext db)
        {
            if (await db.Exercises.AnyAsync())
            {
                return;
            }

            Dictionary<string, int> categoryIds = await GetCategoryIdsAsync(db);

            List<Exercise> exercises = new List<Exercise>
            {
                // Strength Training
                new Exercise
                {
                    Title = "Dumbbell Bench Press",
                    Description = "Lie on a bench with a dumbbell in each hand at chest level. Press the weights up until your arms are extended, then lower them under control.",
                    Difficulty = 3,
                    CategoryId = GetId(categoryIds, "Strength Training")
                },
                new Exercise
                {
                    Title = "Barbell Back Squat",
                    Description = "Set the bar across your upper back and step out from the rack. Squat down until your thighs are at least parallel to the floor, keeping your chest up and knees tracking over your toes, then drive back up.",
                    Difficulty = 5,
                    CategoryId = GetId(categoryIds, "Strength Training")
                },

                // Cardio
                new Exercise
                {
                    Title = "Brisk Walk",
                    Description = "Walk at a pace that raises your heart rate but still lets you hold a conversation. Keep your posture tall, shoulders relaxed, and swing your arms naturally.",
                    Difficulty = 1,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Cardio")
                },
                new Exercise
                {
                    Title = "Jump Rope",
                    Description = "Hold the handles at hip height and turn the rope with your wrists. Jump just high enough to clear the rope, landing softly on the balls of your feet.",
                    Difficulty = 3,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Cardio")
                },

                // HIIT
                new Exercise
                {
                    Title = "Burpee",
                    Description = "From standing, squat down and place your hands on the floor. Jump your feet back into a plank, lower your chest to the floor, push up, jump your feet forward, and explode upward into a jump.",
                    Difficulty = 4,
                    CategoryId = GetId(categoryIds, "HIIT")
                },

                // Yoga
                new Exercise
                {
                    Title = "Downward-Facing Dog",
                    Description = "Start on hands and knees, tuck your toes, and lift your hips up and back. Press your hands into the mat, keep your spine long, and let your head hang between your arms.",
                    Difficulty = 3,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Yoga")
                },

                // Flexibility & Mobility
                new Exercise
                {
                    Title = "Seated Hamstring Stretch",
                    Description = "Sit with one leg extended and the other bent. Hinge forward from the hips toward your extended foot until you feel a gentle stretch in the back of your thigh. Keep your back straight and breathe.",
                    Difficulty = 2,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Flexibility & Mobility")
                },

                // Core
                new Exercise
                {
                    Title = "Plank",
                    Description = "Rest on your forearms and toes with your elbows under your shoulders. Keep your body in a straight line from head to heels, brace your stomach and avoid letting your hips sag or lift.",
                    Difficulty = 2,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Core")
                },

                // Bodyweight
                new Exercise
                {
                    Title = "Bodyweight Squat",
                    Description = "Stand with feet shoulder-width apart. Push your hips back and bend your knees to lower down, keeping your chest up and heels flat. Drive through your feet to return to standing.",
                    Difficulty = 2,
                    CategoryId = GetId(categoryIds, "Bodyweight")
                },
                new Exercise
                {
                    Title = "Push-Up",
                    Description = "Start in a high plank with hands slightly wider than your shoulders. Lower your chest toward the floor while keeping your body in a straight line, then press back up.",
                    Difficulty = 3,
                    CategoryId = GetId(categoryIds, "Bodyweight")
                },

                // Pilates
                new Exercise
                {
                    Title = "Pilates Hundred",
                    Description = "Lie on your back with your legs raised and your head and shoulders lifted. Keep your arms straight by your sides and pump them up and down, breathing in for five pumps and out for five.",
                    Difficulty = 3,
                    CategoryId = GetId(categoryIds, "Pilates")
                },

                // Recovery
                new Exercise
                {
                    Title = "Foam Rolling",
                    Description = "Place the foam roller under the target muscle and use your body weight to roll slowly along it. Pause on tender spots and breathe deeply, avoiding joints and bones.",
                    Difficulty = 1,
                    IsTimed = true,
                    CategoryId = GetId(categoryIds, "Recovery")
                }
            };

            db.Exercises.AddRange(exercises);
            await db.SaveChangesAsync();
        }

        // Seed articles
        private static async Task SeedArticlesAsync(PushDbContext db)
        {
            if (await db.Articles.AnyAsync())
            {
                return;
            }

            List<Article> articles = new List<Article>
            {
                new Article
                {
                    Title = "Meal Prep Basics for a Busy Week",
                    Content = "Spending an hour or two on the weekend preparing meals can make healthy eating much easier during a busy week. Start by choosing two or three simple recipes you enjoy and cook them in larger batches. A good meal prep container has a protein such as chicken, tofu or eggs, a carbohydrate such as rice, pasta or potatoes, and plenty of vegetables. Store meals in the fridge for up to three days and freeze the rest. Keep a few quick snacks on hand, like yoghurt, fruit or nuts, so you're not caught out between meals. Don't aim for perfect. A few prepared meals each week is a great start.",
                    Citation = "PUSH Team",
                    CitationUrl = string.Empty,
                    ImageUrl = "article_placeholder.png",
                    DateTimePublished = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc),
                    Topic = "Meal Prep"
                },

                new Article
                {
                    Title = "What to Eat Before and After a Workout",
                    Content = "What you eat around your workouts can help you train better and recover faster. Around one to three hours before training, have a light meal with carbohydrates for energy and a little protein, such as toast with peanut butter, a banana with yoghurt or a bowl of oats. Avoid heavy, greasy food right before exercising, as it can leave you feeling sluggish. After your workout, aim for a meal with protein to help your muscles recover and carbohydrates to refuel, such as a chicken wrap, eggs on toast or a smoothie with milk and fruit. Drink water throughout the day, not just during your session.",
                    Citation = "PUSH Team",
                    CitationUrl = string.Empty,
                    ImageUrl = "article_placeholder.png",
                    DateTimePublished = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
                    Topic = "Nutrition"
                },

                new Article
                {
                    Title = "Building a Balanced Plate",
                    Content = "A simple way to plan meals without counting anything is to picture your plate in sections. Fill about half with vegetables or salad, a quarter with a protein such as fish, meat, beans or tofu, and a quarter with a carbohydrate such as rice, wholegrain bread or kumara. Add a small amount of healthy fat, like olive oil, avocado or nuts. This approach works for breakfast, lunch and dinner, and it's easy to adjust. Add more carbohydrates on heavy training days and more vegetables on rest days. Try planning your meals a few days ahead so shopping is quicker and you waste less food.",
                    Citation = "PUSH Team",
                    CitationUrl = string.Empty,
                    ImageUrl = "article_placeholder.png",
                    DateTimePublished = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc),
                    Topic = "Meal Planning"
                }
            };

            db.Articles.AddRange(articles);
            await db.SaveChangesAsync();
        }

        // Seed workouts
        private static async Task SeedWorkoutsAsync(PushDbContext db)
        {
            if (await db.Workouts.AnyAsync())
            {
                return;
            }

            // Database check for admin and guest
            Dictionary<string, int> categoryIds = await GetCategoryIdsAsync(db);
            User? admin = await db.Users.FirstOrDefaultAsync(u => u.Email == SeedAdminEmail);
            User? guest = await db.Users.FirstOrDefaultAsync(u => u.IsGuest);

            List<Workout> workouts = new List<Workout>();

            if (admin != null)
            {
                // Admin workouts -- must stay private
                workouts.Add(new Workout { Title = "Strength Day", UserId = admin.Id, CategoryId = GetId(categoryIds, "Strength Training"), IsPrivate = true });
                workouts.Add(new Workout { Title = "Morning HIIT", UserId = admin.Id, CategoryId = GetId(categoryIds, "HIIT"), IsPrivate = true });
                workouts.Add(new Workout { Title = "Yoga Flow", UserId = admin.Id, CategoryId = GetId(categoryIds, "Yoga"), IsPrivate = true });
            }

            if (guest != null)
            {
                // Guest workouts -- must stay private
                workouts.Add(new Workout { Title = "Beginner Full Body", UserId = guest.Id, CategoryId = GetId(categoryIds, "Bodyweight"), IsPrivate = true });
                workouts.Add(new Workout { Title = "Quick Cardio", UserId = guest.Id, CategoryId = GetId(categoryIds, "Cardio"), IsPrivate = true });
                workouts.Add(new Workout { Title = "Gentle Stretch", UserId = guest.Id, CategoryId = GetId(categoryIds, "Flexibility & Mobility"), IsPrivate = true });
            }

            if (workouts.Count > 0)
            {
                db.Workouts.AddRange(workouts);
                await db.SaveChangesAsync();
            }
        }

        // Seed workout exercises
        private static async Task SeedWorkoutExercisesAsync(PushDbContext db)
        {
            if (await db.WorkoutExercises.AnyAsync())
            {
                return;
            }

            // Look ups
            Dictionary<string, int> workoutIds = new Dictionary<string, int>();

            foreach (Workout workout in await db.Workouts.ToListAsync())
            {
                workoutIds[workout.Title] = workout.Id;
            }

            Dictionary<string, int> exerciseIds = new Dictionary<string, int>();

            foreach (Exercise exercise in await db.Exercises.ToListAsync())
            {
                exerciseIds[exercise.Title] = exercise.Id;
            }

            List<WorkoutExercise> workoutExercises = new List<WorkoutExercise>();

            // Local add function
            void Add(string workoutTitle, string exerciseTitle, int sortOrder, int sets, int reps)
            {
                int workoutId;
                int exerciseId;

                // Look ups
                if (!workoutIds.TryGetValue(workoutTitle, out workoutId) || !exerciseIds.TryGetValue(exerciseTitle, out exerciseId))
                {
                    System.Diagnostics.Debug.WriteLine("Seed skipped: " + workoutTitle + " / " + exerciseTitle);
                    return;
                }

                // Add workout exercise
                workoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workoutId,
                    ExerciseId = exerciseId,
                    SortOrder = sortOrder,
                    Sets = sets,
                    Reps = reps
                });
            }

            // Local add function -- for timed workouts
            void AddTimed(string workoutTitle, string exerciseTitle, int sortOrder, int minutes)
            {
                int workoutId;
                int exerciseId;

                // Look ups
                if (!workoutIds.TryGetValue(workoutTitle, out workoutId) || !exerciseIds.TryGetValue(exerciseTitle, out exerciseId))
                {
                    System.Diagnostics.Debug.WriteLine("Seed skipped: " + workoutTitle + " / " + exerciseTitle);
                    return;
                }

                // Add workout exercise
                workoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workoutId,
                    ExerciseId = exerciseId,
                    SortOrder = sortOrder,
                    DurationMinutes = minutes
                });
            }

            // Admin workouts
            Add("Strength Day", "Barbell Back Squat", 1, 4, 8);
            Add("Strength Day", "Dumbbell Bench Press", 2, 4, 8);
            Add("Strength Day", "Push-Up", 3, 3, 12);

            Add("Morning HIIT", "Burpee", 1, 4, 10);
            AddTimed("Morning HIIT", "Jump Rope", 2, 5);
            Add("Morning HIIT", "Bodyweight Squat", 3, 3, 20);

            AddTimed("Yoga Flow", "Downward-Facing Dog", 1, 5);
            Add("Yoga Flow", "Pilates Hundred", 2, 1, 100);
            AddTimed("Yoga Flow", "Seated Hamstring Stretch", 3, 5);

            // Guest workouts
            Add("Beginner Full Body", "Bodyweight Squat", 1, 3, 12);
            Add("Beginner Full Body", "Push-Up", 2, 3, 8);
            AddTimed("Beginner Full Body", "Plank", 3, 3);

            AddTimed("Quick Cardio", "Brisk Walk", 1, 10);
            AddTimed("Quick Cardio", "Jump Rope", 2, 5);
            Add("Quick Cardio", "Burpee", 3, 3, 8);

            AddTimed("Gentle Stretch", "Seated Hamstring Stretch", 1, 5);
            AddTimed("Gentle Stretch", "Downward-Facing Dog", 2, 5);
            AddTimed("Gentle Stretch", "Foam Rolling", 3, 10);

            if (workoutExercises.Count > 0)
            {
                db.WorkoutExercises.AddRange(workoutExercises);
                await db.SaveChangesAsync();
            }
        }

        // Seed notes
        private static async Task SeedNotesAsync(PushDbContext db)
        {
            if (await db.Notes.AnyAsync())
            {
                return;
            }

            List<Workout> workouts = await db.Workouts.ToListAsync();
            List<Note> notes = new List<Note>();

            void Add(string workoutTitle, string title, string content, int daysAgo, int hour)
            {
                Workout? workout = workouts.FirstOrDefault(w => w.Title == workoutTitle);

                if (workout == null)
                {
                    System.Diagnostics.Debug.WriteLine("Seed skipped: note " + title + " / " + workoutTitle);
                    return;
                }

                notes.Add(new Note
                {
                    Title = title,
                    Content = content,
                    DateTimeCreated = SeedDate(daysAgo, hour),
                    UserId = workout.UserId,
                    WorkoutId = workout.Id
                });
            }

            // Admin notes
            Add("Strength Day", "New Squat Weight",
                "Moved up 2.5 kg on the back squat today and all four sets felt solid. My depth was good on every rep, but the last set slowed down, so I'll stay at this weight next session before going up again. Bench felt strong too.",
                12, 7);

            Add("Morning HIIT", "Energy Was High",
                "Felt great this morning and pushed harder than last time. My breathing recovered faster between the burpee sets, which tells me the earlier sessions are paying off. Remember to drink more water before the session.",
                6, 6);

            Add("Yoga Flow", "Yoga Reflection",
                "This session really helped me relax. My shoulders and lower back were tight when I started and felt looser by the end. Holding downward dog a little longer showed me where I'm stiffest. I'd like to make this a regular part of my week after harder workouts.",
                3, 18);

            // Guest notes
            Add("Beginner Full Body", "My First Workout",
                "Got through my first workout today and it felt good to finally get started. I took it slowly to focus on my form. The plank was harder than I expected, so I'll aim to hold it a little longer each time.",
                10, 17);

            Add("Quick Cardio", "Short but Tough",
                "Only twenty minutes but I was out of breath by the end of the burpees. The brisk walk was a good warm-up. Next time I'll try to keep the jump rope going without stopping.",
                5, 12);

            Add("Gentle Stretch", "Rest Day Stretch",
                "Did this on my rest day after my legs were sore from the full body workout. The hamstring stretch and foam rolling helped a lot, and I felt less stiff the next morning.",
                1, 20);

            if (notes.Count > 0)
            {
                db.Notes.AddRange(notes);
                await db.SaveChangesAsync();
            }
        }

        // Seed workout logs
        private static async Task SeedWorkoutLogsAsync(PushDbContext db)
        {
            if (await db.WorkoutLogs.AnyAsync())
            {
                return;
            }

            List<Workout> workouts = await db.Workouts.ToListAsync();

            // Admin workouts
            Workout? strengthDay = workouts.FirstOrDefault(w => w.Title == "Strength Day");
            Workout? morningHiit = workouts.FirstOrDefault(w => w.Title == "Morning HIIT");
            Workout? yogaFlow = workouts.FirstOrDefault(w => w.Title == "Yoga Flow");

            // Guest workouts
            Workout? beginnerFullBody = workouts.FirstOrDefault(w => w.Title == "Beginner Full Body");
            Workout? quickCardio = workouts.FirstOrDefault(w => w.Title == "Quick Cardio");
            Workout? gentleStretch = workouts.FirstOrDefault(w => w.Title == "Gentle Stretch");

            if (strengthDay == null || morningHiit == null || yogaFlow == null
                || beginnerFullBody == null || quickCardio == null || gentleStretch == null)
            {
                System.Diagnostics.Debug.WriteLine("Workout log seed skipped: seeded workouts not found");
                return;
            }

            List<WorkoutLog> workoutLogs = new List<WorkoutLog>
            {
                // Admin - Week
                CreateWorkoutLog(strengthDay, 0, 45),
                CreateWorkoutLog(morningHiit, 1, 30),
                CreateWorkoutLog(yogaFlow, 2, 60),
                CreateWorkoutLog(strengthDay, 3, 40),
                CreateWorkoutLog(morningHiit, 5, 35),

                // Admin - Month
                CreateWorkoutLog(strengthDay, 8, 45),
                CreateWorkoutLog(yogaFlow, 10, 30),
                CreateWorkoutLog(morningHiit, 13, 60),
                CreateWorkoutLog(strengthDay, 15, 40),
                CreateWorkoutLog(morningHiit, 20, 50),
                CreateWorkoutLog(yogaFlow, 24, 35),
                CreateWorkoutLog(strengthDay, 27, 45),

                // Admin - Year
                CreateWorkoutLog(strengthDay, 40, 50),
                CreateWorkoutLog(morningHiit, 70, 45),
                CreateWorkoutLog(yogaFlow, 100, 60),
                CreateWorkoutLog(strengthDay, 130, 30),
                CreateWorkoutLog(morningHiit, 160, 55),
                CreateWorkoutLog(yogaFlow, 200, 40),
                CreateWorkoutLog(strengthDay, 250, 70),
                CreateWorkoutLog(morningHiit, 300, 45),

                // Guest - Week
                CreateWorkoutLog(beginnerFullBody, 0, 30),
                CreateWorkoutLog(quickCardio, 1, 20),
                CreateWorkoutLog(gentleStretch, 2, 20),
                CreateWorkoutLog(beginnerFullBody, 3, 35),
                CreateWorkoutLog(quickCardio, 5, 25),

                // Guest - Month
                CreateWorkoutLog(beginnerFullBody, 8, 30),
                CreateWorkoutLog(gentleStretch, 10, 20),
                CreateWorkoutLog(quickCardio, 13, 25),
                CreateWorkoutLog(beginnerFullBody, 15, 35),
                CreateWorkoutLog(quickCardio, 20, 20),
                CreateWorkoutLog(gentleStretch, 24, 15),
                CreateWorkoutLog(beginnerFullBody, 27, 30),

                // Guest - Year
                CreateWorkoutLog(beginnerFullBody, 40, 30),
                CreateWorkoutLog(quickCardio, 70, 20),
                CreateWorkoutLog(gentleStretch, 100, 20),
                CreateWorkoutLog(beginnerFullBody, 130, 25),
                CreateWorkoutLog(quickCardio, 160, 25),
                CreateWorkoutLog(gentleStretch, 200, 15),
                CreateWorkoutLog(beginnerFullBody, 250, 30),
                CreateWorkoutLog(quickCardio, 300, 20)
            };

            db.WorkoutLogs.AddRange(workoutLogs);
            await db.SaveChangesAsync();
        }

        // Local create workout log function
        private static WorkoutLog CreateWorkoutLog(Workout workout, int daysAgo, int minutes)
        {
            return new WorkoutLog
            {
                WorkoutId = workout.Id,
                UserId = workout.UserId,
                DateTimeCompleted = SeedDate(daysAgo, 7),
                DurationSeconds = minutes * 60
            };
        }

        // Seed weight logs
        private static async Task SeedWeightLogsAsync(PushDbContext db)
        {
            if (await db.WeightLogs.AnyAsync())
            {
                return;
            }

            User? admin = await db.Users.FirstOrDefaultAsync(u => u.Email == SeedAdminEmail);
            User? guest = await db.Users.FirstOrDefaultAsync(u => u.IsGuest);

            if (admin == null || guest == null)
            {
                System.Diagnostics.Debug.WriteLine("Seed skipped: weight logs (admin or guest not found)");
                return;
            }

            List<WeightLog> weightLogs = new List<WeightLog>
            {
                // Admin - Week
                CreateWeightLog(admin.Id, 0, 80.5),
                CreateWeightLog(admin.Id, 1, 80.2),
                CreateWeightLog(admin.Id, 2, 80.0),
                CreateWeightLog(admin.Id, 3, 79.8),
                CreateWeightLog(admin.Id, 5, 79.5),

                // Admin - Month
                CreateWeightLog(admin.Id, 8, 79.3),
                CreateWeightLog(admin.Id, 10, 79.0),
                CreateWeightLog(admin.Id, 13, 78.8),
                CreateWeightLog(admin.Id, 15, 78.5),
                CreateWeightLog(admin.Id, 20, 78.3),
                CreateWeightLog(admin.Id, 24, 78.0),
                CreateWeightLog(admin.Id, 27, 77.8),

                // Admin - Year
                CreateWeightLog(admin.Id, 40, 77.5),
                CreateWeightLog(admin.Id, 70, 77.3),
                CreateWeightLog(admin.Id, 100, 77.0),
                CreateWeightLog(admin.Id, 130, 76.8),
                CreateWeightLog(admin.Id, 160, 76.5),
                CreateWeightLog(admin.Id, 200, 76.3),
                CreateWeightLog(admin.Id, 250, 76.0),
                CreateWeightLog(admin.Id, 300, 75.8),

                // Guest - Week
                CreateWeightLog(guest.Id, 0, 68.0),
                CreateWeightLog(guest.Id, 1, 68.2),
                CreateWeightLog(guest.Id, 2, 68.3),
                CreateWeightLog(guest.Id, 3, 68.5),
                CreateWeightLog(guest.Id, 5, 68.6),

                // Guest - Month
                CreateWeightLog(guest.Id, 8, 68.8),
                CreateWeightLog(guest.Id, 10, 69.0),
                CreateWeightLog(guest.Id, 13, 69.3),
                CreateWeightLog(guest.Id, 15, 69.5),
                CreateWeightLog(guest.Id, 20, 69.8),
                CreateWeightLog(guest.Id, 24, 70.0),
                CreateWeightLog(guest.Id, 27, 70.2),

                // Guest - Year
                CreateWeightLog(guest.Id, 40, 70.5),
                CreateWeightLog(guest.Id, 70, 71.0),
                CreateWeightLog(guest.Id, 100, 71.4),
                CreateWeightLog(guest.Id, 130, 71.8),
                CreateWeightLog(guest.Id, 160, 72.1),
                CreateWeightLog(guest.Id, 200, 72.5),
                CreateWeightLog(guest.Id, 250, 72.8),
                CreateWeightLog(guest.Id, 300, 73.0)
            };

            db.WeightLogs.AddRange(weightLogs);
            await db.SaveChangesAsync();
        }

        // Local create weight log function
        private static WeightLog CreateWeightLog(int userId, int daysAgo, double weightKg)
        {
            return new WeightLog
            {
                UserId = userId,
                DateTimeLogged = SeedDate(daysAgo, 7),
                WeightKg = weightKg
            };
        }
    }
}