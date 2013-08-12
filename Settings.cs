namespace battlecity
{
    class Settings
    {
        public const int MAX_BOARD_SIZE = 200;			                            // Maximum possible board size
        public const int MAX_SOAP_MSG_SIZE = MAX_BOARD_SIZE * MAX_BOARD_SIZE * 16;  // Maximum size of a received SOAP message

        public const long SYNC_TICK = 3000;				                            // Tick period in milliseconds
        public const long SYNC_TARGET = 2800;   	                                // Target usable period between ticks
        public const long SYNC_TARGET_BAND = 400;                                   // Sizes of bands below SYNC_TARGET used to determine adjustment step size
        public const long SYNC_DELTA_STEP_LO = 10;                                  // Sync adjustment step close to SYNC_TARGET
        public const long SYNC_DELTA_STEP_HI = 100;                                 // Sync adjustment step far from SYNC_TARGET
        public const long SYNC_INITIAL_DELAY = 200;                                 // Initial sync delay

        public const long SCHEDULE_EARLY_MOVE = -1000;                              // Time before end of tick to post the backup move
        public const long SCHEDULE_FINAL_MOVE = -500;                               // Time before end of tick to post the final move

        public const int GARBAGE_INTERVAL = 100;		                            // Garbage collection poll interval in milliseconds
        public const int GARBAGE_THRESHOLD_HI = 95;	                                // Percentage total memory use that triggers garbage collection
        public const int GARBAGE_THRESHOLD_LO = 75;	                                // Maximum allowed memory use after garbage collection
    }
}
