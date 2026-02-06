# GooseAPI 🪿

A comprehensive REST API for managing athletes, coaches, workouts, and fitness data integration with Garmin Connect and is tailor made for the GooseNet Platform. Built with .NET 8.0 and Firebase.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Authentication](#authentication)
- [API Endpoints](#api-endpoints)
  - [Authentication & User Management](#authentication--user-management)
  - [Coach-Athlete Connections](#coach-athlete-connections)
  - [Flocks (Athlete Groups)](#flocks-athlete-groups)
  - [Workouts](#workouts)
  - [Sleep Data](#sleep-data)
  - [Training Summary](#training-summary)
  - [Profile Management](#profile-management)
  - [Garmin Integration](#garmin-integration)
- [Data Models](#data-models)
- [Error Handling](#error-handling)
- [Development](#development)
- [Deployment](#deployment)

## Overview

GooseAPI is a fitness coaching platform API that enables:
- **Coaches** to manage athletes, create workout plans, and track performance
- **Athletes** to connect with coaches, log workouts, and view training data
- **Garmin Integration** for automatic workout and sleep data synchronization
- **Google OAuth** for seamless authentication

## Features

- 🔐 JWT-based authentication with Google OAuth support
- 👥 Coach-athlete relationship management
- 🏃 Workout planning and tracking (running & strength)
- 📊 Training summaries and analytics
- 😴 Sleep data tracking via Garmin
- 🦆 Flocks (group management for athletes)
- 📱 Garmin Connect API integration
- 🖼️ Profile picture management

## Technology Stack

- **.NET 8.0** - Web API framework
- **Firebase Realtime Database** - Data storage
- **JWT Bearer Authentication** - Token-based auth
- **Swagger/OpenAPI** - API documentation (development)
- **Garmin Connect API** - Fitness data integration
- **Google OAuth 2.0** - Social authentication

### NuGet Packages

- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.0)
- `Google.Apis.Auth` (1.73.0)
- `FireSharp.Serialization.JsonNet` (1.1.0)
- `Swashbuckle.AspNetCore` (6.4.0)
- `DotNetEnv` (3.1.1)
- `System.Drawing.Common` (9.0.6)

## Prerequisites

- .NET 8.0 SDK
- Firebase project with Realtime Database
- Garmin Connect API credentials (Consumer Key & Secret)
- Google OAuth credentials (Client ID & Secret)
- Environment variables configured (see Configuration)

## Configuration

Create a `.env` file in the project root with the following variables:

```env
# JWT Configuration
Jwt__Secret=your-jwt-secret-key-here
Jwt__Issuer=your-issuer-name
Jwt__Audience=your-audience-name

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GOOGLE_REDIRECT_URI=https://your-domain.com/auth/google/callback

# Firebase Configuration (set in FireBaseConfig.cs)
# Firebase credentials are configured in code
```

### Firebase Setup

Configure Firebase credentials in `FireBaseConfig.cs`:
- Firebase Base Path
- Firebase Secret

### Garmin API Setup

Store Garmin API credentials in Firebase at `/GarminAPICredentials`:
```json
{
  "ConsumerKey": "your-consumer-key",
  "ConsumerSecret": "your-consumer-secret"
}
```

## Authentication

### API Key Authentication

Most endpoints require an `apiKey` query parameter. Each user receives a unique API key upon registration.

### JWT Authentication

For protected endpoints, include the JWT token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

### Getting a JWT Token

1. **Login**: `POST /api/userAuth` - Returns JWT token and API key
2. **Google OAuth**: `GET /auth/google?role=athlete|coach` - Redirects to Google, returns JWT via callback

## API Endpoints

### Authentication & User Management

#### Register User
```http
POST /api/registration
Content-Type: application/json

{
  "userName": "string",
  "email": "string",
  "password": "string",
  "fullName": "string",
  "role": "athlete" | "coach"
}
```

**Response:**
```json
{
  "message": "User Registered Successfully"
}
```

#### Login
```http
POST /api/userAuth
Content-Type: application/json

{
  "userName": "string",
  "hashedPassword": "string"
}
```

**Response:**
```json
{
  "message": "Valid Credentials, Authorized",
  "authorized": true,
  "apiKey": "string",
  "token": "jwt-token-here"
}
```

#### Get Current User
```http
GET /api/userAuth/me
Authorization: Bearer <jwt-token>
```

**Response:** User object (password excluded)

#### Google OAuth Login
```http
GET /auth/google?role=athlete|coach
```

Redirects to Google OAuth, then to `/auth/google/callback` which redirects to:
```
/login/google/success?jwt=<jwt-token>
```

#### Get User Role
```http
GET /api/getRole?apiKey=<api-key>
```

**Response:**
```json
{
  "role": "athlete" | "coach"
}
```

---

### Coach-Athlete Connections

#### Connect Athlete to Coach
```http
POST /api/coachConnection/connect
Content-Type: application/json

{
  "apiKey": "string",
  "coachId": "string"
}
```

**Response:**
```json
{
  "athleteUserName": "string",
  "coachUserName": "string"
}
```

#### Get Coach Name by ID
```http
GET /api/coachConnection/getCoachName?coachId=<coach-id>
```

**Response:**
```json
{
  "coachUsername": "string"
}
```

#### Get Coach ID by Name
```http
GET /api/coachConnection/getCoachId?coachName=<coach-name>
```

**Response:**
```json
{
  "coachId": "string"
}
```

#### Get Athletes (Coach Only)
```http
GET /api/athletes?apiKey=<api-key>
```

**Response:**
```json
{
  "athletesData": [
    {
      "athleteName": "string",
      "imageData": "base64-image-string"
    }
  ]
}
```

---

### Flocks (Athlete Groups)

#### Get Flocks
```http
GET /api/flocks/getFlocks?apiKey=<api-key>
```

**Response:**
```json
{
  "flocks": ["flock-name-1", "flock-name-2"]
}
```

#### Create Flock
```http
POST /api/flocks/createFlock?apiKey=<api-key>&flockName=<flock-name>
```

**Response:**
```json
{
  "message": "Flock created successfully"
}
```

#### Get Flock Athletes
```http
GET /api/flocks/flockAthletes?apiKey=<api-key>&flockName=<flock-name>
```

**Response:** Array of athlete usernames

#### Add Athlete to Flock
```http
POST /api/flocks/addToFlock?apiKey=<api-key>
Content-Type: application/json

{
  "athleteUserName": "string",
  "flockName": "string"
}
```

**Response:**
```json
{
  "message": "athlete added to flock successfully"
}
```

#### Remove Athlete from Flock
```http
POST /api/flocks/removeAthlete?apiKey=<api-key>
Content-Type: application/json

{
  "flockName": "string",
  "athleteName": "string"
}
```

**Response:**
```json
{
  "message": "Athlete was removed from the FLOCK successfully!"
}
```

#### Get Potential Flocks
```http
GET /api/flocks/getPotentialFlocks?apikey=<api-key>&athleteName=<athlete-name>
```

**Response:**
```json
{
  "potentialFlocks": ["flock-name-1", "flock-name-2"]
}
```

---

### Workouts

#### Get Workout Summary by Date
```http
GET /api/workoutSummary?athleteName=<name>&apiKey=<key>&date=<MM/dd/yyyy>
```

**Response:**
```json
{
  "runningWorkouts": [...],
  "strengthWorkouts": [...]
}
```

#### Get Workout by ID
```http
GET /api/workoutSummary/getWorkout?userName=<name>&id=<workout-id>
```

**Response:** Complete workout object

#### Get Workout Data (Laps & Samples)
```http
GET /api/workoutSummary/data?workoutId=<id>&userName=<name>
```

**Response:**
```json
{
  "dataSamples": [...],
  "workoutLaps": [...]
}
```

#### Get Workout Feed (Paginated)
```http
GET /api/workoutSummary/feed?apiKey=<key>&athleteName=<name>&runningCursor=<MM/dd/yyyy>&strengthCursor=<MM/dd/yyyy>
```

**Response:**
```json
{
  "runningWorkouts": [...],
  "strengthWorkouts": [...],
  "runningNextCursor": "MM/dd/yyyy",
  "strengthNextCursor": "MM/dd/yyyy"
}
```

#### Get Workout Laps
```http
GET /api/workoutLaps?userName=<name>&id=<workout-id>
```

**Response:** Array of lap objects

---

### Planned Workouts

#### Get Planned Workouts by Date
```http
GET /api/plannedWorkout/byDate?apiKey=<key>&athleteName=<name>&date=<MM/dd/yyyy>
```

**Response:**
```json
{
  "runningWorkouts": [...],
  "StrengthWorkouts": [...]
}
```

#### Get Planned Workout by ID
```http
GET /api/plannedWorkout/byId?id=<workout-id>
```

**Response:**
```json
{
  "worokutObject": {...},
  "plannedWorkoutJson": "formatted-workout-string"
}
```

#### Get Planned Workout Feed (Paginated)
```http
GET /api/planned/feed?apiKey=<key>&athleteName=<name>&runningCursor=<MM/dd/yyyy>&strengthCursor=<MM/dd/yyyy>
```

**Response:**
```json
{
  "runningWorkouts": [...],
  "strengthWorkouts": [...],
  "runningNextCursor": "MM/dd/yyyy",
  "strengthNextCursor": "MM/dd/yyyy"
}
```

#### Add Running Workout
```http
POST /api/addWorkout?apikey=<api-key>
Content-Type: application/json

{
  "jsonBody": "{...garmin-workout-json...}",
  "date": "yyyy-MM-dd",
  "targetName": "athlete-username",
  "isFlock": false
}
```

**Response:**
```json
{
  "message": "workout pushed successfully"
}
```

**Note:** Workouts are automatically pushed to Garmin Connect if the athlete has connected their account.

---

### Strength Workouts

#### Get Strength Workout by ID
```http
GET /api/strength/workout?id=<workout-id>
```

**Response:** Strength workout object

#### Add Strength Workout
```http
POST /api/strength/addWorkout?apiKey=<api-key>
Content-Type: application/json

{
  "jsonBody": "{...strength-workout-json...}",
  "targetName": "athlete-username-or-flock-name",
  "isFlock": false
}
```

**Response:**
```json
{
  "message": "workout pushed successfully",
  "workoutId": "string"
}
```

#### Get Strength Workout Reviews
```http
GET /api/strength/reviews?apiKey=<key>&workoutId=<id>
```

**Response:** Dictionary of athlete reviews

#### Submit Strength Workout Review
```http
POST /api/strength/reviews?apiKey=<key>&workoutId=<id>
Content-Type: application/json

{
  "athleteName": "string",
  "drills": [...]
}
```

**Response:**
```json
{
  "message": "workout review inserted successfully!"
}
```

---

### Sleep Data

#### Get Sleep Data by Date
```http
GET /api/sleep/byDate?apiKey=<key>&athleteName=<name>&date=<yyyy-MM-dd>
```

**Response:** Sleep data object

#### Get Sleep Data Feed (Paginated)
```http
GET /api/sleep/feed?apiKey=<key>&athleteName=<name>&cursor=<yyyy-MM-dd>
```

**Response:**
```json
{
  "items": [...],
  "nextCursor": "yyyy-MM-dd"
}
```

**Note:** Sleep data requires Garmin connection.

---

### Training Summary

#### Get Training Summary
```http
GET /api/trainingSummary?apiKey=<key>&athleteName=<name>&startDate=<M/d/yyyy>&endDate=<M/d/yyyy>
```

**Response:**
```json
{
  "startDate": "M/d/yyyy",
  "endDate": "M/d/yyyy",
  "distanceInKilometers": 0.0,
  "averageDailyInKilometers": 0.0,
  "timeInSeconds": 0,
  "averageDailyInSeconds": 0.0,
  "allWorkouts": [...]
}
```

---

### Profile Management

#### Change Password
```http
POST /api/editProfile/changePassword?apiKey=<key>
Content-Type: application/json

{
  "NewPassword": "string"
}
```

**Response:**
```json
{
  "message": "password changed successfully"
}
```

#### Change Profile Picture
```http
POST /api/editProfile/changePic?apiKey=<key>&isRevert=false
Content-Type: application/json

{
  "PicString": "base64-image-string"
}
```

**Response:**
```json
{
  "message": "Profile Picture Updated Successfully"
}
```

#### Revert to Default Picture
```http
POST /api/editProfile/changePic?apiKey=<key>&isRevert=true
```

#### Get Profile Picture
```http
GET /api/profilePic?userName=<name>
```

**Response:** Base64 image string

---

### Garmin Integration

#### Request Garmin OAuth Token
```http
GET /api/request-token?apiKey=<key>
```

**Response:**
```json
{
  "stateToken": "string",
  "oauth_token": "string",
  "oauth_token_secret": "string"
}
```

#### Exchange Garmin OAuth Token
```http
GET /api/access-token?oauth_token=<token>&oauth_verifier=<verifier>&token_secret=<secret>&apiKey=<key>
```

**Response:** 200 OK (stores Garmin credentials)

#### Get JWT from State Token
```http
GET /api/auth/stateToken?token=<state-token>
```

**Response:**
```json
{
  "token": "jwt-token"
}
```

#### Validate Garmin Connection
```http
GET /api/ValidateGarminConnection?apiKey=<key>
```

**Response:**
```json
{
  "isConnected": true
}
```

---

### Webhooks

#### Submit Workout Data (Garmin Webhook)
```http
POST /api/webhook/workoutData
Content-Type: application/json

{...garmin-activity-json...}
```

**Response:**
```json
{
  "message": "Workout Stored Successfully"
}
```

#### Submit Sleep Data (Garmin Webhook)
```http
POST /api/webhook/sleepData
Content-Type: application/json

{...garmin-sleep-json...}
```

**Response:** 200 OK

---

## Data Models

### User
```json
{
  "userName": "string",
  "fullName": "string",
  "role": "athlete" | "coach",
  "email": "string",
  "password": "string",
  "profilePicString": "base64-string",
  "defualtPicString": "base64-string",
  "apiKey": "string",
  "googleSubject": "string"
}
```

### Workout
```json
{
  "workoutId": "string",
  "workoutDate": "M/d/yyyy",
  "workoutName": "string",
  "workoutDistanceInMeters": 0,
  "workoutDurationInSeconds": 0,
  "workoutAvgHR": 0,
  "workoutAvgPaceInMinKm": "string",
  "workoutCoordsJsonStr": "string",
  "dataSamples": [...],
  "workoutLaps": [...],
  "userAccessToken": "string"
}
```

### PlannedWorkout
```json
{
  "workoutId": "string",
  "workoutName": "string",
  "description": "string",
  "date": "M/d/yyyy",
  "coachName": "string",
  "athleteNames": ["string"],
  "intervals": [...]
}
```

### StrengthWorkout
```json
{
  "workoutId": "string",
  "workoutName": "string",
  "workoutDate": "M/d/yyyy",
  "coachName": "string",
  "athleteNames": ["string"],
  "workoutReviews": {
    "athleteName": {
      "athleteName": "string",
      "drills": [...]
    }
  }
}
```

### Flock
```json
{
  "flockName": "string",
  "athletesUserNames": ["string"]
}
```

### SleepData
```json
{
  "sleepDate": "yyyy-MM-dd",
  "sleepDurationInSeconds": 0,
  "deepSleepDurationInSeconds": 0,
  "lightSleepDurationInSeconds": 0,
  "remSleepInSeconds": 0,
  "awakeDurationInSeconds": 0,
  "overallSleepScore": 0,
  "sleepScores": {...},
  "sleepStartTimeInSeconds": 0,
  "sleepTimeOffsetInSeconds": 0
}
```

---

## Error Handling

The API returns standard HTTP status codes:

- **200 OK** - Success
- **400 Bad Request** - Invalid request data
- **401 Unauthorized** - Authentication failed or insufficient permissions
- **404 Not Found** - Resource not found

Error responses follow this format:
```json
{
  "message": "Error description"
}
```

---

## Development

### Running Locally

1. Clone the repository
2. Install .NET 8.0 SDK
3. Configure `.env` file with required variables
4. Set up Firebase configuration in `FireBaseConfig.cs`
5. Run the application:
   ```bash
   dotnet run
   ```
6. Access Swagger UI at `https://localhost:<port>/swagger` (development only)

### Project Structure

```
GooseAPI/
├── Controllers/          # API endpoints
├── Program.cs           # Application entry point
├── GooseAPIUtils.cs     # Utility functions
├── FireBaseService.cs   # Firebase integration
├── FireBaseConfig.cs    # Firebase configuration
└── Models/              # Data models
```

---

## Deployment

The API is configured for deployment to Azure App Service. Publish profiles are available in `Properties/PublishProfiles/`.

### Environment Variables

Ensure all environment variables are set in your deployment environment:
- JWT configuration
- Google OAuth credentials
- Firebase credentials

### CORS

The API is configured with CORS policy "AllowAll" for development. **Update this for production** to restrict origins.

---

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

## Support

[Add support information here]

