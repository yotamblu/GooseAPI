using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection.Metadata;

namespace GooseAPI.Controllers
{
    [ApiController]
    public class StrengthWorkoutsController : Controller
    {
        [HttpGet("/api/strength/reviews")]
        public IActionResult GetStrengthWorkoutReview([FromQuery] string apiKey, [FromQuery] string workoutId)
        {
            FirebaseService firebaseService = new FirebaseService();
            //check validation
            string requestingUserName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (requestingUserName == null)
            {
                return BadRequest(new { message = "there is no user found with this apiKey" });
            }
            User requestingUser = GooseAPIUtils.GetUser(requestingUserName);
            StrengthWorkout requestedWorkout = firebaseService.GetData<StrengthWorkout>($"/PlannedStrengthWorkouts/{workoutId}");
            if (requestingUser == null)
            {
                return BadRequest(new { message = "there is no workout with this workout id" });
            }
            if ((requestingUser.Role == "coach" && requestedWorkout.CoachName != requestingUserName) ||
                (requestingUser.Role == "athlete" && !requestedWorkout.AthleteNames.Contains(requestingUserName)))
            {
                return BadRequest(new { message = "you have no access to this data" });
            }
            Dictionary<string, StrengthWorkoutReview> reviews = new Dictionary<string, StrengthWorkoutReview>();

            if (requestedWorkout.WorkoutReviews != null)
            {
                if (requestingUser.Role == "athlete" && requestedWorkout.WorkoutReviews.ContainsKey(requestingUserName))
                {

                    reviews.Add(requestingUserName, requestedWorkout.WorkoutReviews[requestingUserName]);
                }
                else
                {
                    reviews = requestedWorkout.WorkoutReviews;
                }


                return Ok(reviews);
            }
            else
            {
                return Ok(new Dictionary<object, object>());
            }

        }

        [HttpPost("/api/strength/reviews")]
        public IActionResult AddStrengthWorkoutReview([FromQuery] string apiKey, [FromQuery] string workoutId, [FromBody] StrengthWorkoutReview workoutReview)
        {
            FirebaseService firebaseService = new FirebaseService();

            // 1. Validate user by API key
            string requestingUserName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (requestingUserName == null)
                return BadRequest(new { message = "there is no user found with this apiKey" });

            User requestingUser = GooseAPIUtils.GetUser(requestingUserName);

            // 2. Validate workout
            StrengthWorkout requestedWorkout = firebaseService.GetData<StrengthWorkout>($"/PlannedStrengthWorkouts/{workoutId}");
            if (requestedWorkout == null)
                return BadRequest(new { message = "there is no workout with this workout id" });

            // 3. Access rules
            // coaches cannot submit reviews
            if (requestingUser.Role == "coach")
                return BadRequest(new { message = "coaches cannot submit workout reviews" });

            // athlete must be assigned to the workout
            if (!requestedWorkout.AthleteNames.Contains(requestingUserName))
                return BadRequest(new { message = "you are not assigned to this workout" });

            // athlete cannot submit on behalf of someone else
            if (requestingUserName != workoutReview.AthleteName)
                return BadRequest(new { message = "athlete name mismatch" });

            // 4. Insert review
            firebaseService.InsertData($"PlannedStrengthWorkouts/{workoutId}/WorkoutReviews/{requestingUserName}", workoutReview);

            return Ok(new { message = "workout review inserted successfully!" });
        }




        [HttpPost("/api/strength/addWorkout")]
        public IActionResult AddStrengthWorkout([FromQuery] string apiKey, [FromBody] PlannedWorkoutData requestData)
        {
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (userName == null || GooseAPIUtils.GetUser(userName).Role != "coach")
            {
                return Unauthorized(new { message = "this apiKey is not fitting to this action" });
            }


            string id = GooseAPIUtils.GenerateShortHexId();
            StrengthWorkout workoutData = JsonConvert.DeserializeObject<StrengthWorkout>(requestData.jsonBody);

            workoutData.CoachName = userName;
            if (requestData.isFlock)
            {
                List<string> flockAthletes = new FirebaseService().GetData<List<string>>($"/Flocks/{userName}/{requestData.targetName}/athletesUserNames");
                workoutData.AthleteNames = flockAthletes;
            }
            else
            {
                workoutData.AthleteNames = new List<string>() { requestData.targetName };
            }
            workoutData.WorkoutId = id;
            StorePlannedStrengthWorkout(workoutData, id);

            return Ok(new { message = "workout pushed successfully", workoutId = id });
        }



        [HttpGet("/api/strength/workout")]

        public IActionResult GetStrengthWorkoutById([FromQuery] string id) {
            FirebaseService firebaseService = new FirebaseService();

            StrengthWorkout workout = firebaseService.GetData<StrengthWorkout>($"/PlannedStrengthWorkouts/{id}");
            if(workout == null)
            {
                return Unauthorized(
                    new {message = "There is no Strength Workout With this ID"}
                    );
            }


            return Ok(workout);

        
        }



        [NonAction]

        public void StorePlannedStrengthWorkout(StrengthWorkout workout,string id)
        {
            new FirebaseService().InsertData($"PlannedStrengthWorkouts/{id}",workout);
        }


    }
}
