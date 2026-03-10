// --------------------
// Pin Definitions
// --------------------
#define ENCODER_A 2
#define ENCODER_B 3
#define BIKE_PIN 4
#define MIC_PIN A0

// SET TO 1 FOR UNITY CONNECTION. SET TO 0 FOR CONSOLE OUTPUT TESTING
#define DEBUG 1

// --------------------
// Encoder Variables
// --------------------
volatile long encoderPosition = 0;

// --------------------
// Bike Sensor Variables
// --------------------
const int debounce = 200;     
int timeSinceLastRevolution = 999999;
int bikeRevolution = 0;
int prevState = 0;

#if DEBUG
  int bikeCount = 0;
#endif

// --------------------
// Mic Hit Detection
// --------------------
const int micThreshold = 500;      // Adjust experimentally
const unsigned long hitCooldown = 100; // ms between hits
unsigned long lastHitTime = 0;
int hitDetected = 0;

#if DEBUG
  int micCount = 0;
#endif

// --------------------
// Encoder Interrupt
// --------------------
void handleEncoder() {
  int b = digitalRead(ENCODER_B);
  if (b == HIGH) {
    encoderPosition++;
  } else {
    encoderPosition--;
  }
}

// --------------------
// Setup
// --------------------
void setup() {
  #if DEBUG
    // Print to console
    Serial.begin(9600);
  #else
    Serial.begin(115200);
  #endif
  
  pinMode(ENCODER_A, INPUT_PULLUP);
  pinMode(ENCODER_B, INPUT_PULLUP);
  pinMode(BIKE_PIN, INPUT_PULLUP);

  attachInterrupt(digitalPinToInterrupt(ENCODER_A), handleEncoder, CHANGE);
}

// --------------------
// Loop
// --------------------
void loop() {

  // ---- Contact Mic Hit Detection ----
  int micValue = analogRead(MIC_PIN);

  if (micValue > micThreshold) {
    unsigned long currentTime = millis();

    if (currentTime - lastHitTime > hitCooldown) {
      hitDetected = 1;
      lastHitTime = currentTime;

      #if DEBUG
        micCount++;
      #endif
    }
  }

  // ---- Bike Sensor ----
  int currentState = analogRead(BIKE_PIN);

  if(currentState == 0 && prevState != 0){
    if(timeSinceLastRevolution > debounce){
      timeSinceLastRevolution = 0;
      bikeRevolution = 1;

      #if DEBUG
        bikeCount++;
      #endif
    }
  }
  timeSinceLastRevolution++;

  // ---- Send Data ----
  #if DEBUG
    // Print counts
    Serial.print(encoderPosition);
    Serial.print(",");
    Serial.print(bikeCount);
    Serial.print(",");
    Serial.println(micCount);
  #else
    // Send data
    Serial.print(encoderPosition);
    Serial.print(",");
    Serial.print(bikeRevolution);
    Serial.print(",");
    Serial.println(hitDetected);
  #endif

  // Reset flags after sending
  hitDetected = 0;
  bikeRevolution = 0;

  delay(10);  // ~100Hz update rate
}