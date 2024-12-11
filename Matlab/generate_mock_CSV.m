% Prompt inputs
sampling_freq = input('Enter the sampling frequency (Hz): ');
sine_freq = input('Enter the sine wave frequency (Hz): ');
amplitude = input('Enter the amplitude of the sine wave: ');
duration = input('Enter the duration of the sine wave (seconds): ');
filename = input('Enter the name of the output CSV file (e.g., "sine_wave.csv"): ', 's');

% Generate sine wave and save to CSV
generate_sine_csv(sampling_freq, sine_freq, amplitude, duration, filename);
