namespace SohatNotebook.Configuration.Messages
{
    public static class MessageErrors
    {
        public static class Generic
        {
            public static string SomethinsWentWrong = "Something went wrong, please try again later";
            public static string UnableToProcess = "Unable to process request";
            public static string NotFound = "Not found";
            public static string ObjectNotFound = "Object not found";
            public static string BadRequest = "Bad request";
            public static string InvalidPayload = "Invalid payload";
            public static string InvalidRequest = "Invalid request";
        }

        public static class Profile
        {
            public static string UserNotFound = "User is not found";
        }

        public static class Users
        {
            public static string UserNotFound = "User is not found";
        }
    }
}
