bodycam-verb-enable = Turn bodycam on
bodycam-verb-disable = Turn bodycam off
bodycam-examine-enabled = The recording light is [color=red]on[/color].
bodycam-examine-disabled = The recording light is off.
bodycam-popup-enabled = Bodycam recording started
bodycam-popup-disabled = Bodycam recording stopped
bodycam-pocket-denied = The bodycam can't record from inside a pocket.

bodycam-record-name = #{ $id } { $name }
bodycam-wearer-none = nobody
bodycam-wearer-job = { $name } ({ $job })

bodycam-footage-window-title = Bodycam footage
bodycam-footage-round-time = Shift time: { $time }
bodycam-footage-available = Archive online
bodycam-footage-unavailable = Archive offline
bodycam-footage-cameras = Bodycams
bodycam-footage-camera-on = { $name } [recording]
bodycam-footage-camera-off = { $name }
bodycam-footage-segments = Recorded periods
bodycam-footage-no-segments = This bodycam has not recorded anything this shift.
bodycam-footage-segment = { $from } – { $to } · { $wearer }
bodycam-footage-segment-open = { $from } – now · { $wearer }
bodycam-footage-from = From
bodycam-footage-to = To
bodycam-footage-last = Last
bodycam-footage-minutes = minutes
bodycam-footage-length = Length { $length } (max { $max } min)
bodycam-footage-length-too-long = [color=red]Too long[/color]: { $length } (max { $max } min)
bodycam-footage-unfiltered = Unfiltered (admin): everything the server recorded
bodycam-footage-request = Request footage
bodycam-footage-watch = Watch
bodycam-footage-watch-note = Watching disconnects you while the footage plays; you reconnect automatically afterwards.

bodycam-footage-status-idle = Select a bodycam and a time range.
bodycam-footage-status-queued = Waiting in the archive queue...
bodycam-footage-status-processing = Extracting footage...
bodycam-footage-status-sending = Downloading footage...
bodycam-footage-status-sent = Footage ready. Press "Watch".
bodycam-footage-status-sent-detail = { $size } MB received.
bodycam-footage-status-failed = Request failed.

bodycam-footage-access-denied = Access denied.
bodycam-footage-error-camera = That bodycam is not in the archive.
bodycam-footage-error-unavailable = The footage archive is offline: this shift is not being recorded.
bodycam-footage-error-range = Invalid time range.
bodycam-footage-error-too-long = The requested footage is too long. Maximum is { $max } minutes.
bodycam-footage-error-corrupt = The archive file is damaged.
bodycam-footage-error-version = The archive was recorded by a different server version.
bodycam-footage-error-empty = There is no recorded footage in that time range yet.
bodycam-footage-error-internal = The archive failed to extract the footage.
bodycam-footage-error-disconnected = The requester disconnected before the footage was delivered.
bodycam-footage-disconnect-reason = Watching bodycam footage

cmd-bodycam_cut-desc = Cuts a clip out of a server replay file.
cmd-bodycam_cut-help = Usage: bodycam_cut <replay file inside the replay folder> <from tick> <to tick> [camera net id]. Without a camera the clip is unfiltered.
cmd-bodycam_cut-missing = Replay file { $path } not found.
cmd-bodycam_cut-done = Wrote { $path } ({ $size } KiB, ticks { $first }–{ $last }) in { $time } s.
