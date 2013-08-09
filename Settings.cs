namespace battlecity
{
    class Settings
    {
        public const int MAX_BOARD_SIZE = 200;			                            // Maximum possible board size
        public const int MAX_SOAP_MSG_SIZE = MAX_BOARD_SIZE * MAX_BOARD_SIZE * 16;  // Maximum size of a received SOAP message

        public const long SYNC_TICK = 3000;				                            // Tick period in milliseconds
        public const long SYNC_EARLY_LATE = 100;		                            // Seperation in milliseconds between the early/late ticks for synchronisation
        public const long SYNC_DELTA = 100;				                            // Fine adjustment interval for the early-late gate in milliseconds

        public const int GARBAGE_INTERVAL = 100;		                            // Garbage collection poll interval in milliseconds
        public const int GARBAGE_THRESHOLD_HI = 95;	                                // Percentage total memory use that triggers garbage collection
        public const int GARBAGE_THRESHOLD_LO = 75;	                                // Maximum allowed memory use after garbage collection
    }
}
