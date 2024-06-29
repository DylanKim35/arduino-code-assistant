import cv2
import sys
import numpy as np
from numpy import interp
import mediapipe as mp

###################
wCam, hCam = 640, 480
delta_angle_threshold = 5
speed_ratio_threshold = 0.25
###################

###################
output_delta_angle = 0.0
output_speed_ratio = 0.0
###################

# MediaPipe hands 모듈 초기화
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
base_angle_vector = None

cap = cv2.VideoCapture(0)
cap.set(3, wCam)
cap.set(4, hCam)

def calculate_midpoint_4p(p1, p2, p3, p4):
    x = (p1[0] + p2[0] + p3[0] + p4[0]) / 4
    y = (p1[1] + p2[1] + p3[1] + p4[1]) / 4
    return (int(x), int(y))

def calculate_midpoint_3p(p1, p2, p3):
    x = (p1[0] + p2[0] + p3[0]) / 3
    y = (p1[1] + p2[1] + p3[1]) / 3
    return (int(x), int(y))

def calculate_midpoint_2p(p1, p2):
    x = (p1[0] + p2[0]) / 2
    y = (p1[1] + p2[1]) / 2
    return (int(x), int(y))

def max_distance_3p(p1, p2, p3):
    distance_1 = calculate_distance(p1, p2)
    distance_2 = calculate_distance(p2, p3)
    distance_3 = calculate_distance(p3, p1)
    return max(distance_1, distance_2, distance_3)

def calculate_distance(p1, p2):
    return np.linalg.norm(np.array(p1) - np.array(p2))

def unit_vector(vector):
    """ Returns the unit vector of the vector.  """
    return vector / np.linalg.norm(vector)


def angle_between(v1, v2):
    """Returns the angle in radians between vectors 'v1' and 'v2'. The result is negative if v2 is counter-clockwise from v1."""
    v1_u = unit_vector(v1)
    v2_u = unit_vector(v2)

    # 내적을 통해 각도를 구합니다
    dot_product = np.dot(v1_u, v2_u)

    # 외적을 통해 방향성을 구합니다
    cross_product = np.cross(v1_u, v2_u)

    # 내적의 아크코사인으로 각도를 구합니다
    angle = np.arccos(np.clip(dot_product, -1.0, 1.0))

    # 외적의 z 성분을 사용하여 각도의 부호를 결정합니다
    if cross_product > 0:
        angle = -angle

    return np.degrees(angle)


with mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5) as hands:

    while cap.isOpened():
        success, image = cap.read()
        if not success:
          print("Ignoring empty camera frame.")
          # If loading a video, use 'break' instead of 'continue'.
          continue

        # To improve performance, optionally mark the image as not writeable to
        # pass by reference.
        image.flags.writeable = False
        image = cv2.flip(image, 1)
        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        results = hands.process(image_rgb)

        # 랜드마크를 이미지에 그리기
        if results.multi_hand_landmarks:
            hand_landmarks = results.multi_hand_landmarks[0]  # 첫 번째 손만 처리
            mp_drawing.draw_landmarks(
                image, hand_landmarks, mp_hands.HAND_CONNECTIONS)

            height, width, _ = image.shape
            landmarks = [(int(landmark.x * width), int(landmark.y * height)) for landmark in hand_landmarks.landmark]
            tip_2345_midpoint = calculate_midpoint_3p(landmarks[8], landmarks[12], landmarks[16])
            second_2345_midpoint = calculate_midpoint_3p(landmarks[6], landmarks[10], landmarks[14])
            base_2345_midpoint = calculate_midpoint_3p(landmarks[5], landmarks[9], landmarks[13])
            #cv2.circle(image, (tip_345_midpoint[0], tip_345_midpoint[1]), 5, (255, 0, 0), cv2.FILLED)  # 중점을 파란색 원으로 표시
            #cv2.circle(image, (second_2345_midpoint[0], second_2345_midpoint[1]), 5, (255, 0, 0), cv2.FILLED)  # 중점을 파란색 원으로 표시

            origin_base_vector = np.array(base_2345_midpoint) - np.array(landmarks[0])
            origin_second_vector = np.array(second_2345_midpoint) - np.array(landmarks[0])
            origin_tip_vector = np.array(tip_2345_midpoint) - np.array(landmarks[0])

            # 주먹을 쥔 경우
            if np.linalg.norm(origin_tip_vector) < np.linalg.norm(origin_base_vector):
                speed_weight = 1
                origin_pinkyTip_vector = np.array(landmarks[20]) - np.array(landmarks[0])
                origin_pinkyBase_vector = np.array(landmarks[17]) - np.array(landmarks[0])
                # 새끼손가락이 펴져있는 경우
                if np.linalg.norm(origin_pinkyTip_vector) > np.linalg.norm(origin_pinkyBase_vector):
                    speed_weight = -1
                cv2.line(image, landmarks[4], landmarks[6], (0, 255, 0), 3)
                cv2.line(image, second_2345_midpoint, landmarks[0], (255, 0, 255), 3)
                speed_vector = np.array(landmarks[6]) - np.array(landmarks[4])
                base_length = max_distance_3p(landmarks[0], landmarks[5], landmarks[17])
                speed_ratio = min(np.linalg.norm(speed_vector) / base_length, 1.0)
                output_speed_ratio = interp(speed_ratio,[speed_ratio_threshold, 1.0],[0, 1.0]) * speed_weight
                if (base_angle_vector is None):
                    base_angle_vector = origin_second_vector
                delta_angle = angle_between(origin_second_vector, base_angle_vector)
                if np.abs(delta_angle) > delta_angle_threshold:
                    if delta_angle > 0:
                        output_delta_angle = delta_angle - delta_angle_threshold
                    else:
                        output_delta_angle = delta_angle + delta_angle_threshold
                else:
                    output_delta_angle = 0.0

            # 손은 감지되었으나 주먹을 쥐지 않은 경우
            else:
                base_angle_vector = None
                output_delta_angle = 0.0
                output_speed_ratio = 0.0

        else:
            base_angle_vector = None
            output_delta_angle = 0.0
            output_speed_ratio = 0.0

        print(f"{output_delta_angle},{output_speed_ratio}")
        sys.stdout.flush()  # 표준 출력을 즉시 전송하기 위해 flush()를 사용
        cv2.imshow('Hand Tracking', image)
        if cv2.waitKey(5) & 0xFF == 27:
          break


# while True:
#     success, img = cap.read()
#
#     cTime = time.time()
#     fps = 1 / (cTime - pTime)
#     pTime = cTime
#
#     cv2.putText(img, f'FPS: {int(fps)}', (40, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2)
#
#     cv2.imshow('img', img)
#
#     if cv2.waitKey(1) & 0xFF == ord('q'):
#         break

cap.release()
cv2.destroyAllWindows()
