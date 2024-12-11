% Parameters
startDateTime = datetime; % Start date and time
startDateTime.Format = "yyyy-MM-dd  HH:mm:ss";
numPoints = 10000; % Number of data points
frequency = 10; % 1 Sine Cycle Per Day (in Hz)
samplingRate = 1000; % 1 sample per second(10 Hz Sampling)

% Generate time vector
timeVector = startDateTime + seconds(0:samplingRate:(numPoints-1)*samplingRate);

% Generate sine wave data
amplitude = 100; % Amplitude of the sine wave (e.g., in mV)
sineWave = amplitude * sin(2 * pi * frequency * (0:samplingRate:(numPoints-1)*samplingRate));

% Combine time and sine wave data into a table
data = table(timeVector', sineWave', 'VariableNames', {'Timestamp', 'Voltage_mV'});

% Write to CSV
filename = 'NewSin7YYYYMMDD.csv';
writetable(data, filename);

disp(['Data saved to ' filename]);
