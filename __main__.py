from cv2 import VideoCapture, cvtColor, COLOR_BGR2RGB, putText, FONT_HERSHEY_SIMPLEX, circle, imshow, waitKey, destroyAllWindows
import face_recognition
from numpy import mean, degrees, arctan2, isnan
from time import sleep
from threading import Thread

yaw_angle = 0.1
real_yaw_angle = 0.1

def get_yaw_angle(landmarks):
    # Points for eyes and nose bridge for yaw calculation
    left_eye_center = mean(landmarks['left_eye'], axis=0)
    right_eye_center = mean(landmarks['right_eye'], axis=0)
    nose_bridge = mean(landmarks['nose_bridge'], axis=0)

    # Calculate the horizontal displacement between eyes and nose bridge
    dx = right_eye_center[0] - left_eye_center[0]
    dy = nose_bridge[0] - (left_eye_center[0] + dx / 2)

    # Compute yaw angle based on dx and dy
    yaw_angle = degrees(arctan2(dy, dx))
    return yaw_angle

def main():
    global yaw_angle
    global real_yaw_angle
    # Load an image or start video capture
    try:
        video_capture = VideoCapture(0)
    except:
        print("No camera found!! Virtual cameras won't work!!")
        sleep(5)
        exit(-1)

    while True:
        try:
            # Read frame from video capture
            ret, frame = video_capture.read()
            if not ret:
                break
        except:
            print("No camera found!! Virtual cameras won't work!!")
            sleep(5)
            exit(-1)
        
        # Convert the frame to RGB (face_recognition uses RGB format)
        rgb_frame = cvtColor(frame, COLOR_BGR2RGB)

        # Detect face locations and landmarks
        face_locations = face_recognition.face_locations(rgb_frame)
        face_landmarks_list = face_recognition.face_landmarks(rgb_frame, face_locations)

        for face_landmarks in face_landmarks_list:
            # Get yaw angle
            yaw_angle = get_yaw_angle(face_landmarks)

            # Display the yaw angle
            putText(frame, f"Yaw Angle: {yaw_angle:.2f} degrees", 
                        (50, 50), FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
            putText(frame, f"Yaw Angle Interpolated: {real_yaw_angle:.2f} degrees", 
                        (50, 100), FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)

            # Draw landmarks for visualization
            for feature in face_landmarks.keys():
                for point in face_landmarks[feature]:
                    circle(frame, point, 2, (255, 0, 0), -1)

        # Show the result
        imshow('Head Yaw Detection', frame)

        # Break loop with 'q'
        if waitKey(1) & 0xFF == ord('q'):
            break

    # Release resources
    video_capture.release()
    destroyAllWindows()

def InterpolateYaw():
    global real_yaw_angle
    global yaw_angle
    while True:
        if(yaw_angle != 0 or real_yaw_angle != 0):
            try:
                if(yaw_angle < 0):
                    angle_addition = -yaw_angle + 1
                else:
                    angle_addition = 0
                if(yaw_angle > real_yaw_angle):
                    real_yaw_angle += 0.06 * (yaw_angle + angle_addition / real_yaw_angle + angle_addition)
                else:
                    real_yaw_angle -= 0.06 * (real_yaw_angle + angle_addition / yaw_angle + angle_addition)
            except:
                real_yaw_angle = yaw_angle
        sleep(1/60)
        
        print(f"{yaw_angle} | {real_yaw_angle}")

        if(isnan(real_yaw_angle)):
            real_yaw_angle = yaw_angle

        with open("out.txt", 'w') as output:
            output.write(real_yaw_angle)


x = Thread(target=InterpolateYaw)

if __name__ == "__main__":
    x.start()
    main()