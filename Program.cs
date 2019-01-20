using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BitmapHangulFontSnippet
{
    interface IForwardFontRenderer
    {
        void Render(bool bRender);
        void NextLine();
    }

    class ConsoleRenderer : IForwardFontRenderer
    {
        public void Render(bool bRender)
        {
            Console.Write(bRender  ? '#' : ' ');
        }

        public void NextLine()
        {
            Console.WriteLine();
        }
    }

    // 고대의 문서로 부터 복붙 http://mytears.org/resources/doc/Hangul/HANGUL.TXT
    // 초성 20자: 0 ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ
    // 중성 22자: 0 ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ
    // 종성 28자: 0 ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅃㅅㅆㅇㅈㅊㅋㅌㅍㅎ
    //
    // 초성    
    //    초성 1벌 : 받침없는 'ㅏㅐㅑㅒㅓㅔㅕㅖㅣ' 와 결합
    //    초성 2벌 : 받침없는 'ㅗㅛㅡ'
    //    초성 3벌 : 받침없는 'ㅜㅠ'
    //    초성 4벌 : 받침없는 'ㅘㅙㅚㅢ'
    //    초성 5벌 : 받침없는 'ㅝㅞㅟ'
    //    초성 6벌 : 받침있는 'ㅏㅐㅑㅒㅓㅔㅕㅖㅣ' 와 결합
    //    초성 7벌 : 받침있는 'ㅗㅛㅜㅠㅡ'
    //    초성 8벌 : 받침있는 'ㅘㅙㅚㅢㅝㅞㅟ'

    //중성
    //    중성 1벌 : 받침없는 'ㄱㅋ' 와 결합
    //    중성 2벌 : 받침없는 'ㄱㅋ' 이외의 자음
    //    중성 3벌 : 받침있는 'ㄱㅋ' 와 결합
    //    중성 4벌 : 받침있는 'ㄱㅋ' 이외의 자음

    //종성
    //    종성 1벌 : 중성 'ㅏㅑㅘ' 와 결합
    //    종성 2벌 : 중성 'ㅓㅕㅚㅝㅟㅢㅣ'
    //    종성 3벌 : 중성 'ㅐㅒㅔㅖㅙㅞ'
    //    종성 4벌 : 중성 'ㅗㅛㅜㅠㅡ'
    // 
    // 11520 byte = 가로 16 * 세로 16 * (초성 20 * 8벌 + 중성 22 * 4벌 + 종성 28 * 4벌) / 8bit
    class Font
    {
        const int WIDTH = 16;
        const int HEIGHT = 16;
        const int CHO_COUNT = 20;
        const int CHO_TYPE_COUNT = 8;
        const int JUNG_COUNT = 22;
        const int JUNG_TYPE_COUNT = 4;
        const int JONG_COUNT = 28;
        const int JONG_TYPE_COUNT = 4;
        const int GLYPH_BYTES = WIDTH * HEIGHT / 8;
        const int TOTAL_BYTES = WIDTH * HEIGHT * (CHO_COUNT * CHO_TYPE_COUNT + JUNG_COUNT * JUNG_TYPE_COUNT + JONG_COUNT * JONG_TYPE_COUNT) / 8;
        public static readonly int[] choTypesByJungWithoutJong = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 3, 3, 3, 1, 2, 4, 4, 4, 2, 1, 3, 0 };
        public static readonly int[] choTypesByJungWithJong = { 0, 5, 5, 5, 5, 5, 5, 5, 5, 6, 7, 7, 7, 6, 6, 7, 7, 7, 6, 6, 7, 5 };
        public static readonly int[] jongTypesByJung = { 0, 0, 2, 0, 2, 1, 2, 1, 2, 3, 0, 2, 1, 3, 3, 1, 2, 1, 3, 3, 1, 1 };

        private byte[] data;
        
        public Font(string path)
        {
            data = new byte[TOTAL_BYTES];
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            stream.Read(data, 0, TOTAL_BYTES);
        }

        public void PrintChar(char t, IForwardFontRenderer Renderer)
        {
            // 유니코드 한글 자소 분리            
            if (t < '가' || '핳' < t) return;
            
            int baseCode = (t - '가');

            // int Unicode = ChoIndex * ((JUNG_COUNT - 1) * JONG_COUNT) + JungIndex * JONG_COUNT + JongIndex;
            int jongIndex = baseCode % JONG_COUNT;
            int jungIndex = (baseCode / JONG_COUNT) % (JUNG_COUNT - 1) + 1;
            int choIndex = baseCode / ((JUNG_COUNT - 1) * JONG_COUNT) + 1;

            // 벌 선택
            int choType, jungType, jongType;
            if( jongIndex == 0 )
            {
                choType = choTypesByJungWithoutJong[jungIndex];
                jungType = (choIndex == 1 || choIndex == 24) ? 0 : 1;
                jongType = jongTypesByJung[jungIndex];
            }
            else
            {
                choType = choTypesByJungWithJong[jungIndex];
                jungType = (choIndex == 1 || choIndex == 24) ? 2 : 3;
                jongType = jongTypesByJung[jungIndex];
            }

            int choOffset = (choType * CHO_COUNT + choIndex) * GLYPH_BYTES;
            int jungOffset = (CHO_COUNT * CHO_TYPE_COUNT + jungType * JUNG_COUNT + jungIndex) * GLYPH_BYTES;
            int jongOffset = (CHO_COUNT * CHO_TYPE_COUNT + JUNG_COUNT * JUNG_TYPE_COUNT + jongType * JONG_COUNT + jongIndex) * GLYPH_BYTES;

            // 16비트
            for (int j = 0; j < 32; j += 2)
            {
                int choData = data[choOffset + j] << 8 | data[choOffset + j + 1];
                int jungData = data[jungOffset + j] << 8 | data[jungOffset + j + 1];
                int jongData = data[jongOffset + j] << 8 | data[jongOffset + j + 1];

                // 큰비트 -> 작은 비트로
                for (int mask = 0x8000; mask != 0; mask >>= 1)
                {
                    bool bDraw = (choData & mask) != 0 || (jungData & mask) != 0 || (jongData & mask) != 0;
                    Renderer.Render(bDraw);
                }

                Renderer.NextLine();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var consoleRenderer = new ConsoleRenderer();
            var font = new Font(@"SAN.KOR");
            font.PrintChar('퉤', consoleRenderer);
        }
    }
}
