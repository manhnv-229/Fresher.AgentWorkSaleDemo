const exactMatchScore = 3;
const containsMatchScore = 2;
const subsequenceMatchScore = 1;

export interface KnowledgeSearchTextSegment {
  text: string;
  highlighted: boolean;
}

// Chuẩn hóa query và tên file để tìm kiếm không phân biệt khoảng trắng/chữ hoa.
export function normalizeKnowledgeSearchText(value?: string | null): string {
  return (value ?? '').trim().toUpperCase();
}

export function getKnowledgeSearchMatchScore(
  query: string | null | undefined,
  target: string | null | undefined
): number {
  // Điểm ưu tiên: trùng tuyệt đối > chứa chuỗi > khớp theo subsequence.
  const normalizedQuery = normalizeKnowledgeSearchText(query);
  const normalizedTarget = normalizeKnowledgeSearchText(target);

  if (!normalizedQuery || !normalizedTarget) {
    return 0;
  }

  if (normalizedQuery === normalizedTarget) {
    return exactMatchScore;
  }

  if (normalizedTarget.includes(normalizedQuery)) {
    return containsMatchScore;
  }

  return isSubsequence(normalizedQuery, normalizedTarget) ? subsequenceMatchScore : 0;
}

export function isKnowledgeSearchMatch(
  query: string | null | undefined,
  target: string | null | undefined
): boolean {
  // Tách quy tắc tính điểm khỏi nơi sử dụng để các màn chỉ cần kiểm tra có khớp hay không.
  return getKnowledgeSearchMatchScore(query, target) > 0;
}

export function getKnowledgeSearchTextSegments(
  text: string | null | undefined,
  query: string | null | undefined
): KnowledgeSearchTextSegment[] {
  // Trả về các đoạn text có cờ highlighted để template render mà không thay đổi nội dung gốc.
  const sourceText = text ?? '';
  const normalizedText = normalizeKnowledgeSearchText(sourceText);
  const normalizedQuery = normalizeKnowledgeSearchText(query);

  if (!normalizedText || !normalizedQuery) {
    return [{ text: sourceText, highlighted: false }];
  }

  const matchScore = getKnowledgeSearchMatchScore(normalizedQuery, normalizedText);
  if (matchScore === 0 || matchScore === exactMatchScore) {
    return [{ text: sourceText, highlighted: false }];
  }

  if (matchScore === containsMatchScore) {
    const matchIndex = normalizedText.indexOf(normalizedQuery);
    if (matchIndex < 0) {
      return [{ text: sourceText, highlighted: false }];
    }

    return buildRangeSegments(sourceText, matchIndex, normalizedQuery.length);
  }

  return buildSubsequenceSegments(sourceText, normalizedQuery, normalizedText);
}

// Kiểm tra các ký tự query xuất hiện đúng thứ tự trong target, không nhất thiết liền nhau.
function isSubsequence(query: string, target: string): boolean {
  let queryIndex = 0;
  for (let targetIndex = 0; targetIndex < target.length && queryIndex < query.length; targetIndex++) {
    if (target[targetIndex] === query[queryIndex]) {
      queryIndex++;
    }
  }

  return queryIndex === query.length;
}

// Tách một vùng liên tiếp thành trước đoạn khớp, đoạn khớp và sau đoạn khớp.
function buildRangeSegments(text: string, startIndex: number, length: number): KnowledgeSearchTextSegment[] {
  const segments: KnowledgeSearchTextSegment[] = [];
  const before = text.slice(0, startIndex);
  const matched = text.slice(startIndex, startIndex + length);
  const after = text.slice(startIndex + length);

  if (before) {
    segments.push({ text: before, highlighted: false });
  }

  if (matched) {
    segments.push({ text: matched, highlighted: true });
  }

  if (after) {
    segments.push({ text: after, highlighted: false });
  }

  return segments;
}

function buildSubsequenceSegments(
  text: string,
  normalizedQuery: string,
  normalizedText: string
): KnowledgeSearchTextSegment[] {
  // Gom các ký tự subsequence liền nhau thành segment để giảm số node render.
  const highlightIndexes = new Set<number>();
  let queryIndex = 0;
  for (let textIndex = 0; textIndex < normalizedText.length && queryIndex < normalizedQuery.length; textIndex++) {
    if (normalizedText[textIndex] === normalizedQuery[queryIndex]) {
      highlightIndexes.add(textIndex);
      queryIndex++;
    }
  }

  const segments: KnowledgeSearchTextSegment[] = [];
  let currentHighlighted = highlightIndexes.has(0);
  let currentText = text.length > 0 ? text[0] : '';

  for (let index = 1; index < text.length; index++) {
    const isHighlighted = highlightIndexes.has(index);
    if (isHighlighted === currentHighlighted) {
      currentText += text[index];
      continue;
    }

    segments.push({ text: currentText, highlighted: currentHighlighted });
    currentHighlighted = isHighlighted;
    currentText = text[index];
  }

  if (currentText) {
    segments.push({ text: currentText, highlighted: currentHighlighted });
  }

  return segments;
}
