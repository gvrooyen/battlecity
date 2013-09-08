namespace battlecity
{
    class Settings
    {
        public const int MAX_BOARD_SIZE = 200;			                            // Maximum possible board size
        public const int MAX_SOAP_MSG_SIZE = MAX_BOARD_SIZE * MAX_BOARD_SIZE * 16;  // Maximum size of a received SOAP message

        public const long SYNC_TICK = 3000;				                            // Tick period in milliseconds
        public const long SYNC_TARGET = 2700;   	                                // Target usable period between ticks
        public const long SYNC_TARGET_BAND = 400;                                   // Sizes of bands below SYNC_TARGET used to determine adjustment step size
        public const long SYNC_DELTA_STEP_LO = 10;                                  // Sync adjustment step close to SYNC_TARGET
        public const long SYNC_DELTA_STEP_HI = 100;                                 // Sync adjustment step far from SYNC_TARGET
        public const long SYNC_INITIAL_DELAY = 200;                                 // Initial sync delay

        public const long SCHEDULE_EARLY_MOVE = -600;                              // Time before end of tick to post the backup move
        public const long SCHEDULE_FINAL_MOVE = -300;                               // Time before end of tick to post the final move
    }
}
