using Newtonsoft.Json;

namespace GooseAPI
{
    public class ActivityJsonParser
    {
        private readonly string json;
        private const double zoomConstant = 0.076121605;

        public ActivityJsonParser(string json)
        {
            this.json = json;
        }

        public List<Workout> ParseActivityData()
        {
            var activityDetailsList = JsonConvert.DeserializeObject<ActivityDetailsList>(json);
            if (activityDetailsList?.activityDetails == null)
            {
                return new List<Workout>();
            }

            var workouts = new List<Workout>();

            foreach (var summary in activityDetailsList.activityDetails)
            {
                // --- Guard Clauses: Skip processing if data is invalid or not a running workout ---
                if ((summary?.Summary?.ActivityType != "RUNNING" && summary?.Summary.ActivityType != "TREADMILL_RUNNING") || summary.Samples == null || !summary.Samples.Any())
                {
                    continue;
                }
                bool isTreadmill = summary?.Summary.ActivityType == "TREADMILL_RUNNING";
                // --- OPTIMIZATION #1: Sort samples by time ONCE at the beginning ---
                // This is the key. A sorted list allows for extremely fast filtering and lookups.
                var sortedSamples = summary.Samples.OrderBy(s => s.StartTimeInSeconds).ToList();

                var workoutDataSamples = new List<DataSample>();
                var coords = new List<List<float>>();

                // Process all samples in a single pass to build coordinate and sample lists
                foreach (var sample in sortedSamples)
                {
                    DataSample currentDataSample = new DataSample
                    {
                        HeartRate = sample.HeartRate,
                        SpeedMetersPerSecond = sample.SpeedMetersPerSecond,
                        TimerDurationInSeconds = sample.TimerDurationInSeconds
                    };
                    if (!isTreadmill)
                    {
                        currentDataSample.ElevationInMeters = sample.ElevationInMeters;
                    }

                    workoutDataSamples.Add(currentDataSample);
                    if (!isTreadmill && (sample.latitudeInDegree != 0 && sample.longitudeInDegree != 0))
                    {
                        coords.Add(new List<float> { sample.latitudeInDegree, sample.longitudeInDegree });

                    }

                }

                // --- OPTIMIZATION #2: Highly efficient lap processing ---
                // We replaced the slow, second-by-second loop with a fast LINQ query
                // that filters the pre-sorted sample list for each lap's time range.
                var finalLaps = new List<FinalLap>();
                if (summary.Laps != null && summary.Laps.Any())
                {
                    for (int i = 0; i < summary.Laps.Count; i++)
                    {
                        var currentLap = summary.Laps[i];
                        long lapStartTime = currentLap.StartTimeInSeconds;

                        // The end time is the start of the next lap, or the activity's end for the last lap.
                        long lapEndTime = (i + 1 < summary.Laps.Count)
                            ? summary.Laps[i + 1].StartTimeInSeconds
                            : sortedSamples.Last().StartTimeInSeconds;

                        // Use LINQ to efficiently get all samples within this lap. This is lightning fast.
                        var samplesInLap = sortedSamples
                            .Where(s => s.StartTimeInSeconds >= lapStartTime && s.StartTimeInSeconds < lapEndTime)
                            .ToList();

                        if (samplesInLap.Count < 2) // Need at least two points to calculate distance/duration
                        {
                            continue;
                        }

                        // Efficiently calculate stats from the filtered list
                        double avgHeartRate = samplesInLap.Average(s => s.HeartRate);
                        Sample startSample = samplesInLap.First();
                        Sample endSample = samplesInLap.Last();

                        double lapDuration = endSample.TimerDurationInSeconds - startSample.TimerDurationInSeconds;
                        double lapDistance = (endSample.TotalDistanceInMeters - startSample.TotalDistanceInMeters) / 1000.0;

                        if (lapDistance > 0 && lapDuration > 0)
                        {
                            double pace = (lapDuration / 60.0) / lapDistance;
                            finalLaps.Add(new FinalLap
                            {
                                LapDistanceInKilometers = (float)lapDistance,
                                LapDurationInSeconds = (int)lapDuration,
                                LapPaceInMinKm = (float)pace,
                                AvgHeartRate = (int)Math.Round(avgHeartRate)
                            });
                        }
                    }
                }

                // --- Create the final Workout object ---
                var wo = new Workout
                {
                    WorkoutDeviceName = summary.Summary.DeviceName,
                    WorkoutCoordsJsonStr = JsonConvert.SerializeObject(coords),
                    WokroutName = summary.Summary.ActivityName,
                    WorkoutAvgHR = summary.Summary.AverageHeartRateInBeatsPerMinute,
                    WorkoutAvgPaceInMinKm = summary.Summary.AveragePaceInMinutesPerKilometer,
                    WorkoutDistanceInMeters = summary.Summary.DistanceInMeters,
                    WorkoutDurationInSeconds = summary.Summary.DurationInSeconds,
                    WorkoutLaps = finalLaps,
                    WorkoutId = summary.Summary.ActivityId,
                    WorkoutMapCenterJsonStr = JsonConvert.SerializeObject(GenerateCenterFromExtremes(coords)), // Using corrected method
                    WorkoutMapZoom = zoomConstant * summary.Summary.DistanceInMeters,
                    UserAccessToken = summary.UserAccessToken,
                    WorkoutDate = DateTime.Now.ToString("M/d/yyyy"), // Corrected date formatting
                    DataSamples = workoutDataSamples
                };
                workouts.Add(wo);
            }

            return workouts;
        }

        // --- OPTIMIZATION #3: Bug fixes and safety checks in helper method ---
        // This version correctly handles edge cases and fixes the logical bugs from the original.
        private static List<float> GenerateCenterFromExtremes(List<List<float>> coords)
        {
            if (coords == null || coords.Count == 0)
            {
                return new List<float> { 0f, 0f };
            }

            float maxLong = coords[0][0];
            float minLong = coords[0][0];
            float maxLat = coords[0][1];
            float minLat = coords[0][1];

            for (int i = 1; i < coords.Count; i++)
            {
                var coord = coords[i];
                if (coord[0] > maxLong) maxLong = coord[0];
                if (coord[0] < minLong) minLong = coord[0]; // BUG FIX: Was setting minLat
                if (coord[1] > maxLat) maxLat = coord[1];
                if (coord[1] < minLat) minLat = coord[1];
            }

            // BUG FIX: The center is the average (midpoint) of the min and max
            return new List<float>
            {
                (minLong + maxLong) / 2.0f,
                (minLat + maxLat) / 2.0f
            };
        }
    }
}
