function generate_sine_csv(sampling_freq, sine_freq, amplitude, duration, filename)
    t = 0:1/sampling_freq:duration;
    
    % Generate sine wave
    sine_wave = amplitude * sin(2 * pi * sine_freq * t);
    
    % Get current system time
    current_time = datestr(now, 'yyyy-mm-dd HH:MM:SS.FFF');
    
    % Open file for writing
    fid = fopen(filename, 'w');
    if fid == -1
        error('Cannot open file: %s', filename);
    end
    
    % Write the sampling frequency and current time
    fprintf(fid, '%d\n', sampling_freq);
    fprintf(fid, '%s\n', current_time);
    dlmwrite(filename, sine_wave, '-append');
    fclose(fid);
    fprintf('Sine wave data saved to %s\n', filename);
end
