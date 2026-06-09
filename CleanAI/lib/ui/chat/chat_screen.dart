import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'dart:math';
import '../../core/theme/app_colors.dart';
import '../../data/models/chat_message.dart';
import '../../data/repositories/ai_repository.dart';

// State quản lý danh sách tin nhắn và trạng thái loading
class ChatState {
  final List<ChatMessage> messages;
  final bool isAiTyping;
  final String sessionId;

  ChatState({required this.messages, this.isAiTyping = false, required this.sessionId});

  ChatState copyWith({List<ChatMessage>? messages, bool? isAiTyping}) {
    return ChatState(
      messages: messages ?? this.messages,
      isAiTyping: isAiTyping ?? this.isAiTyping,
      sessionId: sessionId,
    );
  }
}

class ChatNotifier extends StateNotifier<ChatState> {
  final AiRepository _aiRepo;

  ChatNotifier(this._aiRepo)
      : super(ChatState(
    messages: [
      ChatMessage(
        id: '0',
        text: "Xin chào! Mình là CleanAI trợ lý ảo. Bạn cần giúp gì hôm nay? 😊",
        isUser: false,
      ),
    ],
    sessionId: _generateSessionId(),
  ));

  static String _generateSessionId() {
    return DateTime.now().millisecondsSinceEpoch.toString() + Random().nextInt(1000).toString();
  }

  Future<void> sendMessage(String text) async {
    // 1. Thêm tin nhắn của User
    final userMsg = ChatMessage(id: DateTime.now().toString(), text: text, isUser: true);
    state = state.copyWith(messages: [...state.messages, userMsg], isAiTyping: true);

    try {
      // 2. Gọi API đến .NET (chờ Ollama trả lời)
      final replyText = await _aiRepo.chatWithBot(state.sessionId, text);

      // 3. Cập nhật tin nhắn của AI
      final aiMsg = ChatMessage(id: DateTime.now().toString(), text: replyText, isUser: false);
      state = state.copyWith(messages: [...state.messages, aiMsg], isAiTyping: false);
    } catch (e) {
      // Báo lỗi nếu rớt mạng
      final errorMsg = ChatMessage(id: DateTime.now().toString(), text: "Lỗi kết nối: ${e.toString()}", isUser: false);
      state = state.copyWith(messages: [...state.messages, errorMsg], isAiTyping: false);
    }
  }
}

final chatProvider = StateNotifierProvider<ChatNotifier, ChatState>((ref) {
  return ChatNotifier(ref.read(aiRepositoryProvider));
});

class ChatScreen extends ConsumerStatefulWidget {
  const ChatScreen({super.key});

  @override
  ConsumerState<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends ConsumerState<ChatScreen> {
  final _controller = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void dispose() {
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _send() {
    final text = _controller.text.trim();
    if (text.isEmpty) return;
    ref.read(chatProvider.notifier).sendMessage(text);
    _controller.clear();
    _scrollToBottom();
  }

  void _scrollToBottom() {
    Future.delayed(const Duration(milliseconds: 100), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final chatState = ref.watch(chatProvider);
    final theme = Theme.of(context);

    // Tự động cuộn khi có tin nhắn mới
    ref.listen(chatProvider, (prev, next) {
      if (prev?.messages.length != next.messages.length) {
        _scrollToBottom();
      }
    });

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              width: 36,
              height: 36,
              decoration: const BoxDecoration(
                color: kPrimaryContainer,
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.auto_awesome_rounded, color: kPrimary, size: 20),
            ),
            const SizedBox(width: 12),
            const Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('CleanAI Assistant', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                Text('Powered by Qwen 2.5', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w400)),
              ],
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
              itemCount: chatState.messages.length + (chatState.isAiTyping ? 1 : 0),
              itemBuilder: (context, i) {
                if (i == chatState.messages.length) {
                  return const Align(
                    alignment: Alignment.centerLeft,
                    child: Padding(
                      padding: EdgeInsets.all(16.0),
                      child: CircularProgressIndicator(),
                    ),
                  );
                }
                return _MessageBubble(message: chatState.messages[i]);
              },
            ),
          ),
          // Input bar
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.06),
                  blurRadius: 8,
                  offset: const Offset(0, -2),
                ),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _controller,
                    enabled: !chatState.isAiTyping, // Khóa input khi AI đang gõ
                    decoration: InputDecoration(
                      hintText: chatState.isAiTyping ? 'AI đang suy nghĩ...' : 'Ask anything...',
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(24),
                        borderSide: BorderSide.none,
                      ),
                      filled: true,
                      fillColor: theme.colorScheme.surfaceContainerHighest,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    ),
                    onSubmitted: (_) => _send(),
                  ),
                ),
                const SizedBox(width: 8),
                GestureDetector(
                  onTap: chatState.isAiTyping ? null : _send,
                  child: Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: chatState.isAiTyping ? Colors.grey : kPrimary,
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(Icons.send_rounded, color: Colors.white, size: 22),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  final ChatMessage message;
  const _MessageBubble({required this.message});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isUser = message.isUser;
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        mainAxisAlignment: isUser ? MainAxisAlignment.end : MainAxisAlignment.start,
        children: [
          if (!isUser) ...[
            Container(
              width: 32,
              height: 32,
              decoration: const BoxDecoration(color: kPrimaryContainer, shape: BoxShape.circle),
              child: const Center(
                child: Text('AI', style: TextStyle(color: kOnPrimaryContainer, fontWeight: FontWeight.w700, fontSize: 11)),
              ),
            ),
            const SizedBox(width: 8),
          ],
          Flexible(
            child: Container(
              constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.75),
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: isUser ? kPrimary : theme.colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.only(
                  topLeft: const Radius.circular(20),
                  topRight: const Radius.circular(20),
                  bottomLeft: Radius.circular(isUser ? 20 : 4),
                  bottomRight: Radius.circular(isUser ? 4 : 20),
                ),
              ),
              child: Text(
                message.text,
                style: TextStyle(
                  color: isUser ? Colors.white : theme.colorScheme.onSurface,
                  fontSize: 15,
                  height: 1.4,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}