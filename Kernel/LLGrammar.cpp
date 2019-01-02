#include "pch.h"
#include "LLGrammar.h"

/// <summary>
/// 移除语句中的空白字符以及制表符
/// </summary>
/// <param name="statement">待处理的语句</param>
/// <returns>格式化语句</returns>
static Statement RemoveSpace(const char statement[])
{
	Statement modifiedStatement{ };
	int length = (int)std::strlen(statement);
	std::for_each(statement, statement + length,
		[&modifiedStatement](char ch) {
		if (ch != ' ' && ch != '\t')
			modifiedStatement.push_back(ch);
	});
	return modifiedStatement;
}

/// <summary>
/// 输入中的非法字符合集
/// </summary>
/// <param name="ch"></param>
/// <returns></returns>
static bool InvalidCharacter(char ch)
{
	return (ch == ' ') || (ch == '\t') || (ch == '.') || (ch == '#') 
		|| (ch == '\0') || (ch == '\n') || (ch == '\r') || (ch == '|');
}

/// <summary>
/// 将给定语句按照特定字符进行分割为若干个子句
/// </summary>
/// <param name="statement"></param>
/// <param name="separator"></param>
/// <returns></returns>
static Statements SplitStringBy(const Statement& statement, char separator)
{
	Statements result{ };
	Statement substatement{ };
	std::for_each(statement.begin(), statement.end(), 
		[&result, &substatement, separator](char ch) {
		if (ch == separator)
		{
			if (substatement.size() != 0)
			{
				result.push_back(substatement);
				substatement.clear();
			}
		}
		else
		{
			substatement.push_back(ch);
		}
	});
	if (substatement.size() > 0)
		result.push_back(substatement);
	return result;
}

static LLGrammar::First GetFirst(
	const LLGrammar::Candidate& candidate, 
	LLGrammar::FirstSet& firstSet
);

static LLGrammar::Follow GetFollow(
	const LLGrammar::Nonterminal& dest,
	const LLGrammar::ProductionSet& productions,
	LLGrammar::FirstSet& firstSet,
	LLGrammar::FollowSet& followSet,
	std::vector<LLGrammar::Nonterminal>& sours,
	std::map<LLGrammar::Nonterminal, bool>& doneList,
	const LLGrammar::Token& start
);

/// <summary>
/// 获取符号集
/// </summary>
/// <returns></returns>
LLGrammar::NoteSet LLGrammar::GetAllNotes() const
{
	return this->notes;
}

/// <summary>
/// 获取产生式集（单候选式）
/// </summary>
/// <returns></returns>
LLGrammar::ProductionSet LLGrammar::GetAllProductions() const
{
	return this->productions;
}

/// <summary>
/// 获取起始非终结符
/// </summary>
/// <returns></returns>
LLGrammar::Nonterminal LLGrammar::GetStartNonterminal() const
{
	return this->start;
}

/// <summary>
/// 向文法中插入一个合法的终结符
/// </summary>
/// <param name="statement">终结符表达式</param>
/// <returns></returns>
bool LLGrammar::InsertTerminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Terminal terminal{ Token::Type::TERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		terminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		terminal
	) == this->notes.end())
	{
		this->notes.insert(terminal);
		return true;
	}
	return false; // 避免重复插入
}

/// <summary>
/// 向文法中插入一个合法的非终结符
/// </summary>
/// <param name="statement">非终结符表达式</param>
/// <returns></returns>
bool LLGrammar::InsertNonterminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		nonterminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	) == this->notes.end())
	{
		this->notes.insert(nonterminal);
		return true;
	}
	return false; // 避免重复插入
}

bool LLGrammar::InsertProduction(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Statements nonterminalAndCandidates
		= SplitStringBy(modifiedStatement, '#');
	if (nonterminalAndCandidates.size() != 2)
		return false; // 非法语句

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(nonterminalAndCandidates[0][pos]))
		nonterminal.attribute.push_back(nonterminalAndCandidates[0][pos++]);
	NoteSet::const_iterator note = std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	);
	if (note == this->notes.end() 
		|| (note->type != Token::Type::NONTERMINAL))
	{
		return false; // 非法产生式
	}

	Statements candidates
		= SplitStringBy(nonterminalAndCandidates[1], '|');
	if (candidates.size() == 0)
		return false; // 无合法候选式

	/* 构造合法产生式 */
	volatile bool failed = false;
	Production production{ nonterminal };
	std::for_each(candidates.begin(), candidates.end(),
		[this, &production, &failed](const Statement& statement) {
		if (!failed)
		{
			Statements tokens
				= SplitStringBy(statement, '.');
			if (tokens.size() == 0)
				return; // 无合法符号

			/* 构造合法候选式 */
			Candidate candidate;
			std::for_each(tokens.begin(), tokens.end(),
				[this, &candidate, &failed](const Statement& statement) {
				if (!failed)
				{
					int pos = 0;
					Token token{ Token::Type::TERMINAL };
					while (!InvalidCharacter(statement[pos]))
						token.attribute.push_back(statement[pos++]);

					NoteSet allNotes = this->GetAllNotes();
					NoteSet::const_iterator exist =
						std::find(allNotes.begin(), allNotes.end(), token);
					if (exist != allNotes.end())
					{
						token.type = exist->type;
						candidate.push_back(token);
					}
					else if (token.attribute.compare({ '@' }) == 0)
					{	// 候选式为Epsilon
						candidate.push_back(token);
					}
					else
					{
						failed = true; // 非法符号 该产生式作废
					}
				}
			});

			/* 插入合法产生式 */
			if (!failed && candidate.size() != 0)
			{
				production.right = candidate;
				this->InsertProduction(production);
			}
		}
	});
	return !failed;
}

/// <summary>
/// 向文法中插入一条合法的产生式
/// </summary>
/// <param name="production"></param>
/// <returns></returns>
bool LLGrammar::InsertProduction(const Production& production)
{
	if (std::find(this->productions.begin(), this->productions.end(), production)
		== this->productions.end())
	{
		this->productions.push_back(production);
		return true;
	}
	return false; // 避免重复插入
}

/// <summary>
/// 设置起始非终结符
/// </summary>
/// <param name="statement"></param>
/// <returns></returns>
bool LLGrammar::SetStartNonterminal(const char statement[])
{
	Statement modifiedStatement
		= RemoveSpace(statement);

	Nonterminal nonterminal{ Token::Type::NONTERMINAL };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		nonterminal.attribute.push_back(modifiedStatement[pos++]);
	if (std::find(
		this->notes.begin(),
		this->notes.end(),
		nonterminal
	) == this->notes.end())
	{
		return false; // 非法非终结符
	}
	this->start = nonterminal;
	return true;
}

/// <summary>
/// 生成一个无左递归、无公共左因子的文法
/// </summary>
/// <returns></returns>
LLGrammar LLGrammar::ToLL() const
{
	LLGrammar llGrammar = *this;
	llGrammar.isLL = true;
	llGrammar.EliminateLeftRecursion();
	llGrammar.EliminateCommonLeftFactor();
	return llGrammar;
}

#define DEBUG
std::string LLGrammar::GetAnalysisSheet()
{
	if (!isLL)
		*this = this->ToLL();

	/* 计算FIRST集以及FOLLOW集 */
	FirstSet firstSet = WorkOutFirstSet();
	if (!firstSet.size())
		return false;

	FollowSet followSet = WorkOutFollowSet(firstSet);
	if (!followSet.size())
		return false;

	/* 建立分析表的行列索引 */
	this->rows.clear();
	NonterminalSet nonterminals;
	int rowIndex = 0;
	for (const auto& token : this->notes)
	{
		if (token.type == Token::Type::NONTERMINAL)
		{
			nonterminals.insert(token);
			this->rows[token] = rowIndex++;
		}
	}

	TerminalSet terminals;
	terminals.insert(Token{ Token::Type::TERMINAL, '$' });
	std::for_each(this->notes.begin(), this->notes.end(),
		[&terminals](const Token& token) {
		if (token.type == Token::Type::TERMINAL)
			terminals.insert(token);
	});

	int columnIndex = 0;
	for (const auto& token : terminals)
		columns[token] = columnIndex++;

	/* 构造LL(1)分析表 */
	for (const auto& production : this->productions)
	{
		First first = GetFirst(production.right, firstSet);
		for (const auto& elem : first)
		{
			if (elem.attribute.compare({ '@' }) == 0)
			{
				for (const auto& elem : followSet[production.left])
				{
					if (sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].left.attribute.compare({ '@' }) != 0)
					{
						dpsheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].push_back(production);
					}
					else
					{
						sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}]
							= production;
					}
				}
			}
			else
			{
				if (sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].left.attribute.compare({ '@' }) != 0)
				{
					dpsheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}].push_back(production);
				}
				else
				{
					sheet[std::pair<int, int>{this->rows[production.left], this->columns[elem]}]
						= production;
				}
			}
		}
	}

#ifdef DEBUG
	/* 输出LL(1)分析表 */
	std::string description = { };
	description += " \t";
	for (const auto& terminal : terminals)
	{
		description += terminal.attribute;
		description += '\t';
	}
	description += '\n';

	for (const auto& nonterminal : nonterminals)
	{
		description += nonterminal.attribute;
		description += '\t';

		for (int column = 0; column < columnIndex; ++column)
		{
			if (sheet[std::pair<int, int>{this->rows[nonterminal], column}].left.attribute.compare({ '@' })
				== 0)
			{
				description += " \t";
			}
			else
			{
				description += sheet[std::pair<int, int>{this->rows[nonterminal], column}].left.attribute;
				description += "→";

				for (const auto& token : sheet[std::pair<int, int>{this->rows[nonterminal], column}].right)
				{
					if (token.attribute.compare({ '@' }) == 0)
						description += "ε";
					else
						description += token.attribute;
				}

				if (dpsheet[std::pair<int, int>{this->rows[nonterminal], column}].size() > 0)
				{
					for (const auto& elem : dpsheet[std::pair<int, int>{this->rows[nonterminal], column}])
					{
						description += " · ";
						description += elem.left.attribute;
						description += "→";

						for (const auto& token : elem.right)
						{
							if (token.attribute.compare({ '@' }) == 0)
								description += "ε";
							else
								description += token.attribute;
						}
					}
				}
				description += '\t';
			}
		}
		description += '\n';
	}

#endif // DEBUG
	return description;
}

std::string LLGrammar::Analyze(const char _Word[])
{
	Statements words
		= SplitStringBy(_Word, ' ');
	/* 修正输入词序列 */
	Tokens tokens;
	Token terminal{ Token::Type::TERMINAL };
	for (const auto& elem : words)
	{
		terminal.attribute = elem;
		tokens.push_back(terminal);
	}
	tokens.push_back({ Token::Type::TERMINAL, '$' });
	tokens.erase(std::find(tokens.begin(), tokens.end(), Token{ Token::Type::TERMINAL, '$' }) + 1,
		tokens.end());

	std::string description = { };
	description += "Step\tStack\tInput\tOutput\n";

	Tokens analysisStack;
	analysisStack.push_back(Token{ Token::Type::TERMINAL, '$' });
	analysisStack.push_back(this->start);
	int step = 1;
	char buf[BUFSIZ] = { 0 };
	int pos = 0;
	do
	{
		/* 步骤 */
		sprintf(buf, "%d", step);
		description += buf;
		description += '\t';
		step++;

		/* 栈 */
		for (const auto& elem : analysisStack)
		{
			description += elem.attribute;
			description += ' ';
		}
		description += '\t';

		/* 输入 */
		for (size_t i = pos; i < tokens.size(); ++i)
		{
			description += tokens[i].attribute;
			description += ' ';
		}
		description += '\t';

		Token top = analysisStack.back();
		if (top.type == Token::Type::TERMINAL)
		{
			if (top.attribute.compare(tokens[pos].attribute) == 0)
			{
				analysisStack.pop_back();
				++pos;
				description += " \n";
			}
			else
			{
				/* 输出 */
				description += "记号不存在\n";
				return description;
			}
		}
		else
		{
			if (tokens[pos].attribute.compare("$") != 0
				&& std::find(this->notes.begin(), this->notes.end(), tokens[pos])
				== this->notes.end())
			{
				/* 输出 */
				description += "记号不存在\n";
				return description;
			}
			else
			{
				if (sheet[std::pair<int, int>{this->rows[top], this->columns[tokens[pos]]}].left.attribute.compare({ '@' })
					!= 0)
				{
					/* 输出 */
					description += sheet[std::pair<int, int>{this->rows[top], this->columns[tokens[pos]]}].left.attribute + "→";
					for (const auto& elem : sheet[std::pair<int, int>{this->rows[top], this->columns[tokens[pos]]}].right)
					{
						if (elem.attribute.compare({ '@' }) == 0)
							description += "ε";
						else
							description += elem.attribute;
					}
					description += '\n';

					/* 动作 */
					analysisStack.pop_back();
					for (Candidate::const_reverse_iterator it
						= sheet[std::pair<int, int>{this->rows[top], this->columns[tokens[pos]]}].right.rbegin();
						it != sheet[std::pair<int, int>{this->rows[top], this->columns[tokens[pos]]}].right.rend(); ++it)
					{
						if (it->attribute.compare({ '@' }) != 0)
						{
							analysisStack.push_back(*it);
						}
					}
				}
				else
				{
					NoteSet::const_iterator it = std::find_if(this->notes.begin(), this->notes.end(),
						[&tokens, &pos](const Token& elem) {
						return elem == tokens[pos];
					});
					if (it != this->notes.end()
						&& it->type == Token::Type::NONTERMINAL
						&& top == *it)
					{
						analysisStack.pop_back();
						++pos;
						description += " \n";
					}
					else
					{
						/* 输出 */
						description += "动作不存在\n";
						return description;
					}
				}
			}
		}
	} while (analysisStack.back().attribute.compare("$") != 0 && pos < (int)tokens.size());
	/* 步骤 */
	sprintf(buf, "%d", step + 1);
	description += buf;
	description += '\t';
	step++;

	/* 栈 */
	for (const auto& elem : analysisStack)
	{
		description += elem.attribute;
	}
	description += '\t';

	/* 输入 */
	for (size_t i = pos; i < tokens.size(); ++i)
		description += tokens[i].attribute;
	if (pos == tokens.size())
		description += ' ';
	description += '\t';

	/* 输出 */
	description += " \n";

	return description;
}

/// <summary>
/// 消除左递归
/// </summary>
void LLGrammar::EliminateLeftRecursion()
{
	ProductionSet oldProductions
		= this->productions;
	this->productions.clear();

	NoteSet allNotes
		= this->notes;

	/* 重建文法 */
	std::for_each(this->notes.begin(), this->notes.end(), 
		[this, &oldProductions, &allNotes](const Token& nonterminal) {
		if (nonterminal.type == Token::Type::NONTERMINAL)
		{
			/* 采集以nonterminal生成的候选式 */
			Candidates alphas;
			Candidates betas;
			std::for_each(oldProductions.begin(), oldProductions.end(),
				[&nonterminal, &alphas, &betas](Production& production) {
				if (production.left == nonterminal)
				{
					if (production.right[0] == nonterminal)
					{
						production.right.erase(production.right.begin());
						alphas.push_back(production.right);
					}
					else
					{
						betas.push_back(production.right);
					}
				}
			});

			if (alphas.size() > 0)
			{
				/* 处理左递归 */
				Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
				newNonterminal.attribute = nonterminal.attribute;
				do
				{
					newNonterminal.attribute += '\'';
				} while (std::find(allNotes.begin(), allNotes.end(), newNonterminal) != allNotes.end());
				allNotes.insert(newNonterminal);

				this->InsertProduction({ newNonterminal, { Token{ } } });
				for (auto& candidate : alphas)
				{
					candidate.push_back(newNonterminal);
					this->InsertProduction({ newNonterminal, candidate });
				}
				for (auto& candidate : betas)
				{
					candidate.push_back(newNonterminal);
					this->InsertProduction({ nonterminal, candidate });
				}
			}
			else
			{
				for (const auto& candidate : betas)
				{
					this->InsertProduction({ nonterminal, candidate });
				}
			}
		}
	});

	this->notes = allNotes;
}

/// <summary>
/// 消除公共左因子
/// </summary>
void LLGrammar::EliminateCommonLeftFactor()
{
	bool noCommonFactor;
	do
	{
		noCommonFactor = true;
		ProductionSet oldProductions
			= this->productions;
		this->productions.clear();

		NoteSet allNotes
			= this->notes;

		/* 重建文法 */
		std::for_each(this->notes.begin(), this->notes.end(), 
			[this, &oldProductions, &allNotes, &noCommonFactor](const Token& nonterminal) {
			if (nonterminal.type == Token::Type::NONTERMINAL)
			{
				/* 采集nonterminal生成的候选式 */
				Candidates candidates;
				std::for_each(oldProductions.begin(), oldProductions.end(), [&nonterminal, &candidates](Production& production) {
					if (production.left == nonterminal)
					{
						candidates.push_back(production.right);
					}
				});
				std::vector<Candidates::const_iterator> merge; // 待合并项
				std::vector<Candidates::const_iterator> rest; // 保留项
				for (Candidates::const_iterator main = candidates.begin(); main != candidates.end(); ++main)
				{
					for (Candidates::const_iterator sub = candidates.begin(); sub != candidates.end(); ++sub)
					{
						if (main != sub && main->front() == sub->front())
							merge.push_back(sub);
						else if (main != sub)
							rest.push_back(sub);
					}
					if (merge.size() > 0)
					{ // 找到某一项的开头存在公共左因子
						merge.push_back(main);
						break;
					}
					else
					{ // 查找下一项
						rest.clear();
					}
				}
				if (merge.size() > 0)
				{
					/* 保留当前不含左因子的候选式 */
					std::for_each(rest.begin(), rest.end(), [this, &nonterminal](Candidates::const_iterator& candidate) {
						this->InsertProduction({ nonterminal, *candidate });
					});

					/* 合并公共左因子 */
					Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
					newNonterminal.attribute = nonterminal.attribute;
					do
					{
						newNonterminal.attribute += '\'';
					} while (std::find(allNotes.begin(), allNotes.end(), newNonterminal) != allNotes.end());
					allNotes.insert(newNonterminal);

					this->InsertProduction({ nonterminal, { merge[0]->front(), { newNonterminal } } });
					std::for_each(merge.begin(), merge.end(), [this, &newNonterminal](Candidates::const_iterator& candidate) {
						Candidate newCandidate = *candidate;
						newCandidate.erase(newCandidate.begin());
						if (newCandidate.size() == 0)
							/* 插入空产生式 */
							this->InsertProduction({ newNonterminal , { Token{ } } });
						else
							this->InsertProduction({ newNonterminal, newCandidate });
					});

					noCommonFactor = false;
				}
				else
				{
					/* 无公共左因子 */
					/* 保留当前不含左因子的候选式 */
					std::for_each(candidates.begin(), candidates.end(), [this, &nonterminal](const Candidate& candidate) {
						this->InsertProduction({ nonterminal, candidate });
					});
				}
			}
		});

		this->notes = allNotes;
	} while (!noCommonFactor);
}

void LLGrammar::Extend()
{
	Nonterminal newNonterminal{ Token::Type::NONTERMINAL };
	newNonterminal.attribute = this->start.attribute;
	do
	{
		newNonterminal.attribute += '\'';
	} while (std::find(this->notes.begin(), this->notes.end(), newNonterminal) != this->notes.end());
	this->notes.insert(newNonterminal);

	this->productions.push_back({ {newNonterminal}, {this->start} });
	this->start = newNonterminal;
}

/// <summary>
/// 递归采集某个非终结符的FIRST集
/// </summary>
/// <param name="candidate"></param>
/// <param name="firstSet"></param>
/// <returns></returns>
static LLGrammar::First GetFirst(const LLGrammar::Candidate& _Candidate, LLGrammar::FirstSet& _Firsts)
{
	LLGrammar::First first;
	LLGrammar::Token front = _Candidate.front();
	if (front.type == LLGrammar::Token::Type::TERMINAL)
	{
		first.insert(front);
	}
	else if (front.type == LLGrammar::Token::Type::NONTERMINAL)
	{
		if (_Firsts[front].size() > 0)
		{ // 第一个非终结符的FIRST集已知 直接合并
			bool epsilon = false;
			std::for_each(_Firsts[front].begin(), _Firsts[front].end(),
				[&epsilon, &first](const LLGrammar::Terminal& elem) {
				if (elem.attribute.compare({'@'}) == 0)
					epsilon = true;
				else
					first.insert(elem); // 合并非空元素
			});
			if (epsilon)
			{
				/* 可导空 */
				LLGrammar::Candidate newCandidate = _Candidate;
				newCandidate.erase(newCandidate.begin());
				if (newCandidate.size() == 0)
				{
					first.insert(LLGrammar::Token{ }); // 无后继项 插入epsilon
				}
				else
				{
					LLGrammar::First more = GetFirst(newCandidate, _Firsts);
					if (more.size() == 0)
					{
						return { }; // 递归过程中碰见FIRST集非法项
					}
					else
					{ // FIRST集合法
						for (const auto& elem : more)
						{
							first.insert(elem);
						}
					}
				}
			}
		}
		else
		{
			// 起始非终结符的FIRST集未知 则当前非终结符也无法求出（避免无限递归）
			// 返回空集以表征该非终结符的FIRST集非法
			return { };
		}
	}
	else if (front.attribute.compare({'@'}) == 0)
	{
		first.insert(LLGrammar::Terminal{});
	}
	return first;
}

/// <summary>
/// 递归采集某个非终结符的FOLLOW集
/// </summary>
/// <param name="dest">待求非终结符</param>
/// <param name="productions">产生式集合</param>
/// <param name="firstSet"></param>
/// <param name="followSet"></param>
/// <param name="sours">已经访问过的非终结符</param>
/// <param name="doneList">FOLLOW集已知的非终结符</param>
/// <returns></returns>
static LLGrammar::Follow GetFollow(
	const LLGrammar::Nonterminal& _Dest,
	const LLGrammar::ProductionSet& _Productions,
	LLGrammar::FirstSet& _Firsts, 
	LLGrammar::FollowSet& _Follows, 
	std::vector<LLGrammar::Nonterminal>& _Sours,
	std::map<LLGrammar::Nonterminal, bool>& _DoneList,
	const LLGrammar::Token& _Start
)
{
	LLGrammar::Follow follow;
	LLGrammar::Candidate candidate;
	for (const auto& production : _Productions)
	{
		LLGrammar::Follow more;
		LLGrammar::Candidate::const_iterator next = production.right.begin();
		while ((next = std::find(next, production.right.end(), _Dest)) != production.right.end())
		{
			if (next != production.right.end())
			{ // 待求非终结符出现在该产生式中
				++next;
				bool epsilon = false;
				if (next != production.right.end())
				{
					LLGrammar::Candidate candidate{ next, production.right.end() };
					more = GetFirst(candidate, _Firsts);
					for (const auto& elem : more)
					{
						if (elem.attribute.compare({'@'}) == 0)
							epsilon = true;
						else
							follow.insert(elem);
					}
				}
				if (epsilon
					|| next == production.right.end())
				{
					/* 合并产生式左部的FOLLOW集 */
					if (_DoneList[production.left])
					{ // 左部的FOLLOW集已知
						for (const auto& elem : _Follows[production.left])
						{
							follow.insert(elem);
						}
					}
					else if (production.left != _Sours[0]
						&& production.left != _Dest)
					{   // 未知则仅当左部非终结符不是第一个待求非终结符或与当前待求非终结符不同时才进行递归
						// 以避免无限递归
						if (std::find(_Sours.begin() + 1, _Sours.end(), production.left)
							== _Sours.end())
						{ // 若该非终符未被递归访问 则插入已访问集中
							_Sours.push_back(production.left);
							more = GetFollow(
								production.left,
								_Productions,
								_Firsts, _Follows,
								_Sours, _DoneList,
								_Start
							);
							if (_Sours.size() == 0)
								return { }; // 求解过程失败
							else
							{
								_Sours.pop_back();
								if (production.left == _Start)
									follow.insert(
										LLGrammar::Token{ LLGrammar::Token::Type::TERMINAL, {'$'} }
								);
								for (const auto& elem : more)
									follow.insert(elem);
							}
						}
						else
						{   // 出现无限递归
							// 将已访问集合置为空以标志当前求解过程失败
							_Sours.clear();
							return { };
						}
					}
				}
			}

			if (next != production.right.end())
				++next;
		}
	}
	return follow;
}

/// <summary>
/// 求解FIRST集
/// </summary>
/// <returns></returns>
LLGrammar::FirstSet LLGrammar::WorkOutFirstSet() const
{
	FirstSet firstSet;
	ProductionSet oldProductions
		= this->productions;
	bool isDone;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&firstSet, &isDone, &oldProductions](const Token& nonterminal) {
			if (nonterminal.type == LLGrammar::Token::Type::NONTERMINAL)
			{
				/* 采集FIRST集 */
				First first;
				std::for_each(oldProductions.begin(), oldProductions.end(),
					[&firstSet, &nonterminal, &first](const Production& production) {
					if (production.left == nonterminal)
					{
						First more = GetFirst(production.right, firstSet);
						for (const auto& elem : more)
							first.insert(elem);
					}
				});

				for (const auto& elem : first)
				{
					if (firstSet[nonterminal].find(elem) == firstSet[nonterminal].end())
					{
						firstSet[nonterminal].insert(elem);
					}
				}
			}
		});
	} while (++count <= (int)this->notes.size() * (int)this->notes.size());
	return firstSet;
}

/// <summary>
/// 求解FOLLOW集
/// </summary>
/// <param name="firstSet"></param>
/// <returns></returns>
LLGrammar::FollowSet LLGrammar::WorkOutFollowSet(FirstSet& _First) const
{
	FollowSet followSet;
	followSet[this->start].insert(Token{ Token::Type::TERMINAL, '$' });

	std::map<Token, bool> doneList;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&doneList, this, &_First, &followSet](const Token& nonterminal) {
			if (nonterminal.type == Token::Type::NONTERMINAL
				&& doneList[nonterminal] == false)
			{
				/* 采集FOLLOW集 */
				std::vector<Nonterminal> sours{ nonterminal };
				Follow follow
					= GetFollow(
						nonterminal,
						this->GetAllProductions(),
						_First, followSet,
						sours, doneList,
						this->GetStartNonterminal()
					);
				for (const auto& elem : follow)
				{
					followSet[nonterminal].insert(elem);
					doneList[nonterminal] = true;
				}
			}
		});

	} while (++count <= 2 * (int)this->notes.size());
	return followSet;
}
