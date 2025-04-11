% Parameters
% startDateTime = datetime; % Start date and time
% startDateTime.Format = "yyyy-MM-dd HH:mm:ss";
startDateTime = datetime("2025-04-01 00:00:00", "InputFormat", "yyyy-MM-dd HH:mm:ss");
startDateTime.Format = "yyyy-MM-dd HH:mm:ss";  % Optional: for display

numPoints = 17280; % Number of data points
frequency = 0.00003; % Frequency in Hz
samplingRate = 0.1; % Sampling rate in Hz (samples per second)

% Generate time vector
timeVector = startDateTime + seconds(0 : (1/samplingRate) : (numPoints-1) * (1/samplingRate));

% Generate channel numbers (1-4 cyclically)
channelNumbers = mod(0:numPoints-1, 4) + 1; % Repeats [1, 2, 3, 4]

% Define amplitude and phase offsets for each channel
amplitude = 100; % Amplitude in mV
phaseOffsets = [0, pi/12, pi/6, pi/4]; % 0°, 15°, 30°, 45° offsets

% Generate sine wave data for each channel
sineWave = amplitude * sin(2 * pi * frequency * (0:(1/samplingRate):(numPoints-1)*(1/samplingRate)) + phaseOffsets(channelNumbers));

% Combine into a table (with Channel between Timestamp and Data)
data = table(timeVector', channelNumbers', sineWave', 'VariableNames', {'Timestamp', 'Channel', 'Voltage_mV'});

% Write to CSV
filename = 'April_1_2_final_2_mock.csv';
writetable(data, filename);

disp(['Data saved to ' filename]);
