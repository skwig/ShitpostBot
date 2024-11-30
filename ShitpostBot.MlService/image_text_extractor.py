import cv2
import pytesseract


class ImageTextExtractor:

    def __init__(self) -> None:
        pass

    def extract_text(self, img):
        greyscale = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        if (img.shape[0] < 2000 or img.shape[1] < 2000):
            greyscale = cv2.resize(greyscale, (img.shape[1] * 2, img.shape[0] * 2))

        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))

        greyscale_clahe = clahe.apply(greyscale)
        inverted_greyscale_clahe = 255 - greyscale_clahe

        config = r'--psm 11 -c load_system_dawg=0 -c load_freq_dawg=0'
        lang = r'eng+ces+slk'
        final_image = cv2.GaussianBlur(greyscale_clahe, (3, 3), 0)
        # inverted_final_image = cv2.GaussianBlur(inverted_greyscale_clahe, (3, 3), 0)

        return self.__to_lines(pytesseract.image_to_data(final_image, lang=lang, config=config, output_type=pytesseract.Output.DICT)) #\
            # + self.__to_lines(pytesseract.image_to_data(inverted_final_image, lang=lang, config=config, output_type=pytesseract.Output.DICT))

    def __to_lines(self, data: dict):
        # for idx, v in enumerate(data["conf"]):
        #     print("[" + str(idx) + "]: ", end=" ")
        #     for k in data.keys():
        #         print(str(k) + ": " + str(data[k][idx]), end=" ")
        #     print()

        lines: list[str] = []
        line: str = None

        page_num: int = None
        block_num: int = None
        par_num: int = None
        line_num: int = None
        for idx, _ in enumerate(data["conf"]):
            start_new_line = page_num is None or page_num != data["page_num"][idx] \
                             or block_num is None or block_num != data["block_num"][idx] \
                             or par_num is None or par_num != data["par_num"][idx] \
                             or line_num is None or line_num != data["line_num"][idx]

            page_num = data["page_num"][idx]
            block_num = data["block_num"][idx]
            par_num = data["par_num"][idx]
            line_num = data["line_num"][idx]

            if start_new_line:
                if line is not None and len(line) > 0:
                    lines.append(line.strip())

                line = ""

            else:
                filtered_word = self.__filter_word(data["text"][idx], data["conf"][idx])
                if filtered_word is not None:
                    line += " " + filtered_word

                    # finish the line, if it was started
        if line is not None and len(line) > 0:
            lines.append(line.strip())

        return lines

    def __filter_word(self, word: str, confidence: float) -> str:
        if confidence > 90:
            return word
        elif sum(map(str.isalpha, word)) >= 3:
            return word
        else:
            return None
