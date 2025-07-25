﻿using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route($"/api/athletes")]
    public class AthletesController : Controller
    {
        [HttpGet]
        public IActionResult GetAthletes([FromQuery] string apiKey)
        {
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (GooseAPIUtils.GetUser(userName).Role != "coach")
            {
                return Unauthorized(new { message = "only coaches have athletes connected to them" });
            }
            Dictionary<string, AthleteCoachConnection> athleteCoachConnections = new FirebaseService().GetData<Dictionary<string, AthleteCoachConnection>>("/AthleteCoachConnections");
            List<string> athletesList = athleteCoachConnections.Values
            .Where(c => c.CoachUserName == userName)
            .Select(c => c.AthleteUserName)
            .ToList();

            List<AthleteCard> athleteCards = new List<AthleteCard>();
            foreach (string athlete in athletesList)
            {
                athleteCards.Add(new AthleteCard
                {
                    athleteName = athlete,
                    imageData = GooseAPIUtils.GetUser(athlete).ProfilePicString
                });
            }

            
            return Ok(new GetAthletesResponseData { athletesData = athleteCards});
        }
    }
}
